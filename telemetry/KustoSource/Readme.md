# How to write maintainable and readable kusto queries

Codespace service is a distrubuted application. Logs gets streamed from different pods. State transitions are possible via user actions, workers, heartbeat, etc. All the above, makes KQL queries complex. This is an attempt to write readable, maintainable kusto queries. 

This also helps tracks the queries in source control.

## How do I write a query? 
Use Kusto Explorer Client or the Azure Data Explorer to write your query. Often your queries will be based on a well known event which other queries are using.
Those re-usable queries are added to *Events* folder. 
* Create a get<something>.ksf file. With *#function("docstring")* at the first line.
* Run the kc.exe tool, to produce the .csl or .ksl file under the KustoQuery folder.


## Preprocessing tables or Cooking

Some queries are so expensive, that they cannot be directly used on a dashboard. Such queries are pre-processed on a scheduled task to produce an intermediate table. The intermediate tables are usually small and have targetted information, which is easy and fast to query.

## Using the Tool

The Kusto Compiler tool, pre-processes .ksf files into .kql files. It can take an individual file or a directory. Today it only handles `#include` and `#function`.

```
 kc compile -i c:\mywork\vsclk-core\telemetry\KustoSource\ -o c:\mywork\vsclk-core\telemetry\KustoQuery\
```

Executes .csl files so that they are updated in the table. 
Note: 
1) Functions can be built using other functions. If the dependency is not satisfied, it will break.
2) Functions are used directly in dashoards, if you break the functions it will reflect in the dashboards. To revert, you can just sync to master and re-run the tool.
```
kc.exe runFunctionUpdate -i c:\mywork\vsclk-core\telemetry\KustoQuery
```

## Using Generated KQL files in Lens/PowerBI/AzureDataExplorer Dashboards

Generated queries are scoped to a hardcoded date range and ring. When using the same query on Dashboard, you can copy paste the query, but have to make manual adjustment on defining the filter.