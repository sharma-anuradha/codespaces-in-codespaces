import "@forevolve/bootstrap-dark/dist/css/bootstrap-dark.min.css";
import "@forevolve/bootstrap-dark/dist/css/toggle-bootstrap-dark.min.css";
import "@forevolve/bootstrap-dark/dist/css/toggle-bootstrap-print.min.css";
import "@forevolve/bootstrap-dark/dist/css/toggle-bootstrap.min.css";
import "bootstrap/dist/css/bootstrap.css";
import "bootstrap/dist/css/bootstrap.min.css";
import { Provider } from "mobx-react";
import React from "react";
import ReactDOM from "react-dom";
import "react-grid-layout/css/styles.css";
import "react-resizable/css/styles.css";
import { BrowserRouter } from "react-router-dom";
import actions from "./actions";
import { AppState } from "./appState";
import App from "./components/App";
import Hub from "./hub";
import "./index.css";

declare var module: any;
const appState = new AppState();
appState.LoadFromStorage();
const hub = new Hub();
const stores = {
  appState,
  hub,
};

actions.replaceTheme(actions.getLocalTheme());

const baseUrl = document
  .getElementsByTagName("base")[0]
  .getAttribute("href") as string;
const rootElement = document.getElementById("react-app");
const render = () => {
  ReactDOM.render(
    <Provider {...stores}>
      <BrowserRouter basename={baseUrl}>
        <App />
      </BrowserRouter>
    </Provider>,
    rootElement
  );
};

render();

if (module.hot) {
  module.hot.accept("./components/App", () => {
    render();
  });
}
