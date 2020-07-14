# allow reception of messages via tcp port
<source>
  @type forward
  port 24224
  bind 0.0.0.0
</source>

# allow reception of messages from log files
<source>
  @type tail
  # read any log file that has "default" in its name, that's the namespace we're monitoring
  path /var/log/containers/*default*.log
  pos_file /var/log/fluentd-containers.log.pos
  time_format %Y-%m-%dT%H:%M:%S.%NZ
  tag logfile.*
  format json
  read_from_head false
</source>

# Do not directly collect fluentd's own logs to avoid infinite loops
<match fluent**>
  @type null
</match>

<match logfile.**fluentd**.log>
  @type null
</match>

<match logfile.var.log.containers.**geneva-logger**.log>
  @type null
</match>

<match logfile.var.log.containers.**kube-system**.log>
  @type null
</match>

# Format all messages by extracting the JSON data from the "log" field as expressed
# by VS SaaS SDK's diagnostics logger.
# Also add tag to the record so we can use it as a key in the rewrite_tag_filter filters
<filter logfile.**>
  @type record_modifier
  <record>
    _expand_ ${ if record.has_key?("log"); begin; parsed = JSON.parse(record["log"]); parsed.each do |key, value|; record[key] = value; end; rescue; record["msg"] = record["log"]; end; end; nil }
    tag ${tag}
  </record>
  remove_keys _expand_,log
</filter>

# Tag records that didn't parse as JSON as deletable
<match logfile.**>
  @type rewrite_tag_filter
  <rule>
    key     Service
    pattern ^(.+)$
    tag     vssaas.${tag}
  </rule>
  <rule>
    key     tag
    pattern ^(.+)$
    tag     deletable.${tag}
  </rule>
</match>

# Send all the deletable records to nowhere (the null processor).
<match deletable.**>
  @type null
</match>

# split remaining streams into kubeprobe, auditing, and service-specific streams
<match vssaas.**>
  @type rewrite_tag_filter
  <rule>
    key     HttpRequestUri
    pattern ^/warmup.*$
    tag     kubeprobe.${tag}
  </rule>
  <rule>
    key     HttpRequestUri
    pattern ^/health.*$
    tag     kubeprobe.${tag}
  </rule>
  <rule>
    key     Service
    pattern ^AsmIfxAuditApp$
    tag     ifxauditapp.${tag}
  </rule>
  <rule>
    key     Service
    pattern ^AsmIfxAuditMgmt$
    tag     ifxauditmgmt.${tag}
  </rule>
  <rule>
    key     Service
    pattern ^(.+)$
    tag     $1.${tag}
  </rule>
</match>

# Send all the kubeprobe activity to nowhere (the null processor).
<match kubeprobe.**>
  @type null
</match>

# See https://github.com/Azure/fluentd-plugin-mdsd to configure the following mdsd processors

# Send ifxauditapp as the ifxauditapp source.
<match ifxauditapp.**>
  @type mdsd
  @log_level info
  # Full path to mdsd dynamic json socket file
  djsonsocket /var/run/mdsd/default_djson.socket
  # Max time in milliseconds to wait for mdsd acknowledge response. If 0, no wait.
  acktimeoutms 5000
  # An array of regex patterns for mdsd source name unification purpose.
  # The passed will be matched against each regex, and if there's a match, the matched substring will be used as the resulting mdsd source name.
  mdsd_tag_regex_patterns ["^ifxauditapp"]
  num_threads 1
  buffer_chunk_limit 1000k
  buffer_type file
  buffer_path /var/log/td-agent/buffer/out_ifxauditapp*.buffer
  buffer_queue_limit 128
  flush_interval 10s
  retry_limit 3
  retry_wait 10s
</match>

# Send ifxauditmgmt as the ifxauditmgmt source.
<match ifxauditmgmt.**>
  @type mdsd
  @log_level info
  # Full path to mdsd dynamic json socket file
  djsonsocket /var/run/mdsd/default_djson.socket
  # Max time in milliseconds to wait for mdsd acknowledge response. If 0, no wait.
  acktimeoutms 5000
  # An array of regex patterns for mdsd source name unification purpose.
  # The passed will be matched against each regex, and if there's a match, the matched substring will be used as the resulting mdsd source name.
  mdsd_tag_regex_patterns ["^ifxauditmgmt"]
  num_threads 1
  buffer_chunk_limit 1000k
  buffer_type file
  buffer_path /var/log/td-agent/buffer/out_ifxauditmgmt*.buffer
  buffer_queue_limit 128
  flush_interval 10s
  retry_limit 3
  retry_wait 10s
</match>

# Send envreg as the envreg source.
<match envreg.**>
  @type mdsd
  @log_level info
  # Full path to mdsd dynamic json socket file
  djsonsocket /var/run/mdsd/default_djson.socket
  # Max time in milliseconds to wait for mdsd acknowledge response. If 0, no wait.
  acktimeoutms 5000
  # An array of regex patterns for mdsd source name unification purpose.
  # The passed will be matched against each regex, and if there's a match, the matched substring will be used as the resulting mdsd source name.
  mdsd_tag_regex_patterns ["^envreg"]
  num_threads 1
  buffer_chunk_limit 1000k
  buffer_type file
  buffer_path /var/log/td-agent/buffer/out_envreg*.buffer
  buffer_queue_limit 128
  flush_interval 10s
  retry_limit 3
  retry_wait 10s
</match>

# Send computeprovisioning as the computeprovisioning source.
<match computeprovisioning.**>
  @type mdsd
  @log_level info
  # Full path to mdsd dynamic json socket file
  djsonsocket /var/run/mdsd/default_djson.socket
  # Max time in milliseconds to wait for mdsd acknowledge response. If 0, no wait.
  acktimeoutms 5000
  # An array of regex patterns for mdsd source name unification purpose.
  # The passed will be matched against each regex, and if there's a match, the matched substring will be used as the resulting mdsd source name.
  mdsd_tag_regex_patterns ["^computeprovisioning"]
  num_threads 1
  buffer_chunk_limit 1000k
  buffer_type file
  buffer_path /var/log/td-agent/buffer/out_computeprovisioning*.buffer
  buffer_queue_limit 128
  flush_interval 10s
  retry_limit 3
  retry_wait 10s
</match>

# Send ContainerPoolWorker as the ContainerPoolWorker source.
<match ContainerPoolWorker.**>
  @type mdsd
  @log_level info
  # Full path to mdsd dynamic json socket file
  djsonsocket /var/run/mdsd/default_djson.socket
  # Max time in milliseconds to wait for mdsd acknowledge response. If 0, no wait.
  acktimeoutms 5000
  # An array of regex patterns for mdsd source name unification purpose.
  # The passed will be matched against each regex, and if there's a match, the matched substring will be used as the resulting mdsd source name.
  mdsd_tag_regex_patterns ["^ContainerPoolWorker"]
  num_threads 1
  buffer_chunk_limit 1000k
  buffer_type file
  buffer_path /var/log/td-agent/buffer/out_ContainerPoolWorker*.buffer
  buffer_queue_limit 128
  flush_interval 10s
  retry_limit 3
  retry_wait 10s
</match>

# Send signlr as the signlr source.
<match signlr.**>
  @type mdsd
  @log_level info
  # Full path to mdsd dynamic json socket file
  djsonsocket /var/run/mdsd/default_djson.socket
  # Max time in milliseconds to wait for mdsd acknowledge response. If 0, no wait.
  acktimeoutms 5000
  # An array of regex patterns for mdsd source name unification purpose.
  # The passed will be matched against each regex, and if there's a match, the matched substring will be used as the resulting mdsd source name.
  mdsd_tag_regex_patterns ["^signlr"]
  num_threads 1
  buffer_chunk_limit 1000k
  buffer_type file
  buffer_path /var/log/td-agent/buffer/out_signlr*.buffer
  buffer_queue_limit 128
  flush_interval 10s
  retry_limit 3
  retry_wait 10s
</match>

# Send vsobi as the vsobi source.
<match vsobi.**>
  @type mdsd
  @log_level info
  # Full path to mdsd dynamic json socket file
  djsonsocket /var/run/mdsd/default_djson.socket
  # Max time in milliseconds to wait for mdsd acknowledge response. If 0, no wait.
  acktimeoutms 5000
  # An array of regex patterns for mdsd source name unification purpose.
  # The passed will be matched against each regex, and if there's a match, the matched substring will be used as the resulting mdsd source name.
  mdsd_tag_regex_patterns ["^vsobi"]
  num_threads 1
  buffer_chunk_limit 1000k
  buffer_type file
  buffer_path /var/log/td-agent/buffer/out_vsobi*.buffer
  buffer_queue_limit 128
  flush_interval 10s
  retry_limit 3
  retry_wait 10s
</match>

# Retag to prefix all remaining streams with kubernetes
<match **.vssaas.**>
  @type rewrite_tag_filter
  <rule>
    key     tag
    pattern ^(.+)$
    tag     kubernetes.final
  </rule>
</match>

# Send all remaining streams as the kubernetes source.
<match kubernetes.**>
  @type mdsd
  @log_level info
  # Full path to mdsd dynamic json socket file
  djsonsocket /var/run/mdsd/default_djson.socket
  # Max time in milliseconds to wait for mdsd acknowledge response. If 0, no wait.
  acktimeoutms 5000
  # An array of regex patterns for mdsd source name unification purpose.
  # The passed will be matched against each regex, and if there's a match, the matched substring will be used as the resulting mdsd source name.
  mdsd_tag_regex_patterns ["^kubernetes"]
  num_threads 1
  buffer_chunk_limit 1000k
  buffer_type file
  buffer_path /var/log/td-agent/buffer/out_kubernetes*.buffer
  buffer_queue_limit 128
  flush_interval 10s
  retry_limit 3
  retry_wait 10s
</match>
