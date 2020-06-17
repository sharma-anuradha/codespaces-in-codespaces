import { uniq } from "lodash";
import { CardType } from "./appState";

export class Actions {
  getLocalTheme() {
    const local = this.getFromLocalStorage("localtheme");
    if (local) { return local; }
    if (
      window.matchMedia &&
      window.matchMedia("(prefers-color-scheme: dark)").matches
    ) {
      return "bootstrap-dark";
    }
    else { return "bootstrap"; }
  }

  setLocalTheme(theme: string) {
    if (theme === "bootstrap-dark" || theme === "bootstrap") {
            this.saveToLocalStorage("localtheme", theme);
    }
    this.replaceTheme(theme);
  }

  replaceTheme(theme: string) {
    if (theme === "bootstrap-dark") {
      window.document.body.classList.add("bootstrap-dark");
      window.document.body.classList.remove("bootstrap");
    } else {
      window.document.body.classList.add("bootstrap");
      window.document.body.classList.remove("bootstrap-dark");
    }
  }

  getFromLocalStorage(key: string) {
    let ls;
    if (window.localStorage) {
      try {
        ls = JSON.parse(window.localStorage.getItem(key));
      } catch (e) {
        // TODO: Add error handling popover to expose errors.
        console.log(e);
      }
    }
    return ls;
  }

  saveToLocalStorage(key: string, value: any) {
    if (window.localStorage) {
      const item = JSON.stringify(value);
      window.localStorage.setItem(key, item);
    }
  }

  getColumns(columnsToShow: string, objectCols: string[]) {
    let columnSetup = columnsToShow.split(",");
    if (columnSetup.includes("*")) {
      columnSetup.splice(columnSetup.indexOf("*"), 1);
      columnSetup = columnSetup.concat(objectCols);
    }
    return uniq(columnSetup);
  }

  groupBy(key: string, item: any, items: any[]) {
    if (!item[key]) {
      return;
    }
    const values = items.filter(n => n[key] === item[key]);
    if (values.length <= 0) {
      items.push(item);
    }
    else {
      items[items.indexOf(values[0])] = item;
    }
  }

  // tslint:disable-next-line: max-func-body-length
  generateNewCardsTemplate() {
    return [
      {
        type: CardType.Unknown,
        name: "---",
        description: "Select a card from the dropdown.",
        options: {},
        id: "",
      },
      {
        type: CardType.LogViewer,
        name: "Pool Size",
        description: "Shows the Pool Size based on the pool definitions",
        options: {
          filterList: [
            {
              name: "Message",
              key: "msg",
              value: "watch_pool_state_task_run_unit_check_complete",
              placeHolder: "",
            },
          ],
          columnSettings: "PoolDefinition,PoolSkuName,PoolLocation,PoolResourceType,PoolUnassignedCount,PoolIsEnabled,time",
          groupBy: "PoolDefinition"
        },
        id: "",
      },
      {
        type: CardType.LogViewer,
        name: "JSON Log \"Firehose\"",
        description: "Shows all JSON logs, defaults with no filters",
        options: {
          filterList: [
            {
              name: "Level",
              key: "level",
              value: "",
              placeHolder: "Ex. info,warning,error",
            },
            {
              name: "Service",
              key: "Service",
              value: "",
              placeHolder: "Ex. BackEndWebApi,VSOnline",
            },
          ],
          columnSettings: "level,msg",
        },
        id: "",
      },
      {
        type: CardType.LogViewer,
        name: "Backend JSON Logs",
        description: "Shows JSON logs from the Backend Web Service",
        options: {
          filterList: [
            {
              name: "Level",
              key: "level",
              value: "",
              placeHolder: "Ex. info,warning,error",
            },
            {
              name: "Service",
              key: "Service",
              value: "BackEndWebApi",
              placeHolder: "Ex. BackEndWebApi,VSOnline",
            },
          ],
          columnSettings: "level,msg",
        },
        id: "",
      },
      {
        type: CardType.LogViewer,
        name: "Frontend JSON Logs",
        description: "Shows JSON logs from the Frontend Web Service",
        options: {
          filterList: [
            {
              name: "Level",
              key: "level",
              value: "",
              placeHolder: "Ex. info,warning,error",
            },
            {
              name: "Service",
              key: "Service",
              value: "VSOnline",
              placeHolder: "Ex. BackEndWebApi,VSOnline",
            },
          ],
          columnSettings: "level,msg",
        },
        id: "",
      },
      {
        type: CardType.IsNgrokRunning,
        name: "Is Ngrok Running?",
        description: "Tells you if Ngrok is running...",
        defaultSize: { width: 2, height: 2, minWidth: 2, minHeight: 2 },
        options: {},
        id: "",
      },
      {
        type: CardType.ProcessViewer,
        name: "Process Viewer",
        description: "List of running processes.",
        options: {},
        id: "",
      },
      {
        type: CardType.Debug,
        name: "Debug Card",
        description: "Just a base card, nothing in it.",
        options: {},
        id: "",
      },
    ];
  }
}

const actions = new Actions();

export default actions;
