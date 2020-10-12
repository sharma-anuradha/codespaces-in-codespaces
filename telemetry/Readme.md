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

```
kc.exe runFunctionUpdate -i c:\mywork\vsclk-core\telemetry\KustoQuery
```

## Using Generated KQL files in Lens/PowerBI/AzureDataExplorer Dashboards

Generated queries are scoped to a hardcoded date range and ring. When using the same query on Dashboard, you can copy paste the query, but have to make manual adjustment on defining the filter.

## Notes: 
1) Please try running the kc commands in developer command prompt, if its not recognized in windows command prompt.
2) Functions can be built using other functions. If the dependency is not satisfied, it will break.
3) Functions are used directly in dashboards, if you break the functions it will reflect in the dashboards. To revert, you can just sync to master and re-run the tool.
4) Please dont change the header of the functions. As .ksf files are kusto source files. If they are functions they have to begin with "#function(..)"
5) You can test the kusto functions in kusto explorer for testing purpose.
6) Dont change the csl files directly, csl files are expected to be auto-generated from ksf files after kc compile command.
7) It is recommended to use the following command for your development of queries. This prevents breaking dashboard during development phase.
    
    ```
    kc.exe printInline -i  getAvailableToShuttingDownEvents.ksf 
    ```


