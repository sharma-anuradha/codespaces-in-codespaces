import { observable, observe } from "mobx";
import actions from "./actions";

export class AppState {
  readonly cardTabs = observable<CardTab>([], { deep: true });
  @observable selectedTab: number = 0;
  constructor() {
    observe(this.cardTabs, (change) => {
      this.Save();
    });
  }

  Observe(options: any) {
    return observe(options, (change) => {
      this.Save();
    });
  }

  Save() {
    actions.saveToLocalStorage("log_cards", this.cardTabs);
  }

  LoadFromStorage() {
    const cards = actions.getFromLocalStorage("log_cards");
    console.log(cards);
    if (cards) {
      if (Array.isArray(cards) && cards[0].name) {
        this.cardTabs.replace(cards);
      } else {
         const cardTab = new CardTab();
         cardTab.cards.replace(cards);
         cardTab.layouts = actions.getFromLocalStorage("log_layouts");
         this.cardTabs.replace([cardTab]);
      }
    } else {
      this.cardTabs.replace([new CardTab()]);
    }
  }
}

export class CardTab {
  @observable name: string = "Default";
  readonly cards = observable<any>([], { deep: true });
  @observable layouts: any;
}

export class LogViewerOptions {
  @observable filterList: Filter[] = [];
  @observable columnSettings: string = "level,msg";
  @observable groupBy: string = "";
}

export class Filter {
  @observable name: string = "";
  @observable placeHolder: string = "";
  @observable key: string = "";
  @observable value: string = "";
}

export enum CardType {
  Unknown,
  Debug,
  LogViewer,
  IsNgrokRunning,
  ProcessViewer,
}

export enum EventType {
  Empty,
  Error,
  Info,
}

export class DefaultSize {
  width?: number = 4;
  height?: number = 3;
  minWidth?: number = 1;
  minHeight?: number = 1;
}

export class GridCard {
  id: string = "";
  type: CardType;
  name: string;
  description: string;
  defaultSize?: DefaultSize = new DefaultSize();
  @observable options: any;
}
