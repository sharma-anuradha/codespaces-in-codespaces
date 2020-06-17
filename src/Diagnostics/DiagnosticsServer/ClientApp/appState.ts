import { observable, observe } from 'mobx';
import actions from "./actions";

export class AppState {
   readonly cards = observable<any>([], { deep: true });
   readonly layouts = observable<any>(actions.getFromLocalStorage("log_layouts") || {})

   constructor() {
      observe(this.cards, (change) => {
         this.Save();
      });
   }

   Observe(options: any) {
      return observe(options, (change) => {
         this.Save();
      });
   }

   Save() {
      actions.saveToLocalStorage("log_cards", this.cards);
   }
   
   LoadFromStorage() {
      let cards = actions.getFromLocalStorage("log_cards");
      if (cards)
         this.cards.replace(cards);
   }
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
   ProcessViewer
}

export enum EventType {
   Empty,
   Error,
   Info
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