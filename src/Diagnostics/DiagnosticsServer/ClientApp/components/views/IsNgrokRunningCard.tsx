import {
  faCheckSquare,
  faExclamationTriangle
} from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import Hub from "hub";
import { observable } from "mobx";
import { inject, observer } from "mobx-react";
import React from "react";
import { AppState, EventType } from "../../appState";
import BaseCard from "./BaseCard";

/* 
    'inject' is a mobx-react way to inject datastores from our index.tsx into our components without
    having to pass them in every time. We need to make them nullable so TypeScript won't request 
    we add them when creating a new component. They'll be automatically taken care of by mobx-react.
*/
@inject("hub")
@inject("appState")
/* 
    'observer' is mobx-react syntax to convert our component to a reactive one.
    It's necessary or else none of our observable elements will fire.
*/
@observer
class IsNgrokRunningCard extends React.Component<{
  layoutCard: any;
  appState?: AppState;
  hub?: Hub;
}> {
  /* 
    Any object tagged with '@observable' will cause the 
    'render' method to fire when its object is touched.
    In this case, when we set our 'isRunning' variable from
    the signalR hub event, it will cause the UI to update with
    the right icon.

    However, this also means you can't edit observable elements from within the 'render'
    method, as that will cause mobx to throw an exception since you'll be stuck in a render loop.
  */
  @observable isRunning: boolean = false;
  constructor(props) {
    super(props);
    // Hooks into 'newNgrokEvent' event. When a new item gets logged,
    // the callback will be invoked as a generic 'item'. Inspect that
    // and you can get JSON from the logs.
    this.props.hub.AddNgrokHubCallback((n) => this.monitorLogs(n));
  }

  monitorLogs(item: any) {
    if (item.eventType !== EventType.Info) { return; }
    if (item.eventName !== "isRunning") { return; }
    this.isRunning = item.event.isRunning || false;
  }

  componentWillUnmount() {
    // Remove callbacks when we remove the card, else they'll hang around even though
    // the component was unmounted. While it won't crash the app, it's better to clean up.
    this.props.hub.RemoveNgrokHubCallback((n) => this.monitorLogs(n));
  }

  renderBody() {
    return (
      <FontAwesomeIcon
        style={{ width: "100%", height: "100%" }}
        color={this.isRunning ? "green" : "red"}
        icon={this.isRunning ? faCheckSquare : faExclamationTriangle}
      />
    );
  }

  render() {
    return (
      <BaseCard layoutCard={this.props.layoutCard} body={this.renderBody()} />
    );
  }
}

export default IsNgrokRunningCard;
