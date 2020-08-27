# prom-mdm-converter

## Integrating with Prometheus
The prom-mdm-converter relies on Prometheus' `remote_write` module, which can be used to pipe ALL metrics Prometheus receives to an HTTP endpoint. The prom-mdm-converter then uses the rules its been given to decide whether to fire these metrics to MDM.

If you're using the official Prometheus chart, the configuration override for the values.yaml is fairly straightforward:
```yaml
serverFiles:
  prometheus.yml:
    remote_write:
      - url: "http://<prom-mdm-converter_service_name>.<namespace>.svc.cluster.local/receive"
```
*(Note that the fully qualified `<namespace>.svc.cluster.local` is only necessary if prom-mdm-converter is hosted in a separate Kubernetes namespace.)*

If you deploy this chart, prom-mdm-converter will be in the monitoring namespace, so the configuration would be:
```yaml
serverFiles:
  prometheus.yml:
    remote_write:
      - url: "http://prom-mdm-converter>.monitoring/receive"
```

The default `remote_write` settings seem to be acceptable.

## Gotchas, tips, & tricks
> Replace Geneva Mdm Metric Name from label

By default, one needs to specify all Prometheus metrics to be forwarded to Geneva MDM. This may be impractical in scenarios like forwarding application metrics (which may be numerous) to Geneva.
As a workaround, one can find useful the following approach:

* Send all the metrics from your application as a single metric name (like my_counter)
* Putting desired metric name into the Label
* Apply Prometheus rule `on this single metric` in Prometheus

To support this functionality, we need to add mdm-converter the option to reset metric name to desired one. 
* Prom-mdm-converter will look for the metric with label `GenevaMdmMetricName`
* If found - **metric name will be replaced** by the value from this label (as-is, without case changing or any other formating)


```
rule example:
# delta over 30s since evaluation interval is set to 30s
- record: my_general_counter_delta
  expr: label_replace( (delta(my_general_counter_total[30s])) , "GenevaMdmMetricName", "$1", "MetricName", "(.+)")
```

> MDM Namespace

If not configured other, by default prom-mdm-converter will use `Prometheus` as MDM Geneva namespace for **all** metrics, sent to Geneva.

There are 2 options to change this ***default*** behavior:

* You can specify a `GENEVA_DEFAULT_MDM_NAMESPACE` environment variable to override the mdm namespace used for **all**  metrics if one is not specified as a label in the metric
* Add Label `MdmNamespace` to each user metric - this value will be sent to Geneva for this metric
* The order is: `MdmNamespace` label if present, if not - `GENEVA_DEFAULT_MDM_NAMESPACE` value, if not - default (`Prometheus`)

> Counters in Linux

MANY Linux metrics (platform metrics, request count metrics, etc), are fired to Prometheus as **unadjusted** counter values. These values start at 0, and monotonically count upwards until they eventually overflow, at which point they "reset" back to 0 and continue counting up again. (This is typically done for performance reasons.) With counter values, we're not very interested in the actual raw value, but rather the _rate of increase_ over time.

This means in Prometheus, (and therefore MDM if left unadjusted), the event stream will look something like:
```
15:41:00> { "label1": "foo", "label2": "bar" } : 100
15:42:00> { "label1": "foo", "label2": "bar" } : 100
15:43:00> { "label1": "foo", "label2": "bar" } : 112
15:44:00> { "label1": "foo", "label2": "bar" } : 245
```
Rather than:
```
15:42:00> { "label1": "foo", "label2": "bar" } : 0    // (100 - 100)
15:43:00> { "label1": "foo", "label2": "bar" } : 12   // (112 - 100)
15:44:00> { "label1": "foo", "label2": "bar" } : 133  // (245 - 112)
```
As a general rule, due to Prometheus naming recommendations, if a metric is suffixed with `_total` then it is likely a **counter** that will need to be adjusted before firing to MDM.

In the Prometheus world, you can convert raw counter values into "rate/gauge" values at query time using the `irate()` built-in function. This function essentially takes the current event, subtracts it from the _previous_ value, and calculates a **per-second rate** (more on this later). Unfortunately MDM cannot work this way. MDM never saves the _raw_ values; it only saves per-minute aggregate values. So, MDM _requires_ that all counter values be transformed into "rate/gauge" values in order to do anything meaningful.

Luckily, Prometheus has some built-in functionality to periodically execute queries, and re-fire the results as a new timeseries/metric. You can do this by configuring [recording rules](https://prometheus.io/docs/prometheus/latest/configuration/recording_rules/). Just like the `remote_write` module, this can be easily configured if you're using the official Prometheus chart:
```yaml
serverFiles:
  rules:
    groups:
      - name: my_counter_conversions
        interval: 1m
        rules:
          - record: node_cpu_usage
            expr: "100 - (avg by (nodename) (irate(node_cpu_seconds_total{mode=\"idle\"}[5m]) * on(instance) group_left(nodename) node_uname_info * 100))"
          - record: node_disk_read_bytes_total_rate
            expr: "sum by (device, nodename) (irate(node_disk_read_bytes_total[5m]) * 60 * on(instance) group_left(nodename) node_uname_info)"
```
Once configured, you simply need to instruct prom-mdm-converter to collect these new metrics. Some notes on the above:
* The "on(instance)" section is a Prometheus query concept to join metrics with other metrics at query time. In this case we're joining with `node_uname_info` to transform an IP-based "instance" label (e.g. 10.244.0.1) to a more human-readable "nodename" label (e.g. aks-nodepool1-abc123).
* Notice that we're multiplying the `irate()` value by 60 in the `node_disk_read_bytes_total_rate` metric above. This is because `irate()` automatically returns the **per-second** rate. We aren't very interested in this, because we're simply trying to find the number of bytes read during the given minute. (And we can transform this back to a "rate average" in MDM anyway.) So, we multiply by 60 to get the actual full number of bytes sent during this minute.
* Be careful with the differences between `rate()` and `irate()`. When you specify a time-series query, (above, `node_disk_read_bytes_total[5m]` means "look at least 5 minutes back from the current sample"), `rate()` will take ALL returned values into account. If your counter event fires once a minute (most plaform metrics do), that means `rate()` will return the average rate **across the past 5 minutes of data**.  `irate()`, on the other than, will look **up to** 5 minutes back for the nearest event, but will only ever calculate the rate difference between the most recent 2. (Which is the behavior we were looking for.)

> Identifying/developing metrics to collect

While you can choose to use the open-source tool Grafana to develop your Prometheus queries, Prometheus itself comes with a simple website to do this. If you port-forward to :9090 on the Prometheus server, you can access the endpoint at "`http://localhost:9090/graph`".

Example kubectl command:
```bash
kubectl port-forward -n <namespace> <server pod name> 9090:9090
```