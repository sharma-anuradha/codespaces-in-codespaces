import Hub from "hub";
import { observable } from "mobx";
import { inject, observer } from "mobx-react";
import React from "react";
import { CardText, Table } from "reactstrap";
import Actions from "../../actions";
import { AppState, EventType } from "../../appState";
import BaseCard from "./BaseCard";

@inject("hub")
@inject("appState")
@observer
class ProcessViewerCard extends React.Component<{
  layoutCard: any;
  appState?: AppState;
  hub?: Hub;
}> {
  constructor(props) {
    super(props);
    this.props.hub.AddProcessHubCallback((n) => this.monitorLogs(n));
  }
  @observable items: any[] = [];
  columns: string[] = ["process", "isRunning", "sessionTime", "startTime"];
  monitorLogs(item: any) {
    if (item.type === EventType.Error) {
      return;
    }
    Actions.groupBy("process", item, this.items);
  }

  componentWillUnmount() {
    this.props.hub.RemoveProcessHubCallback((n) => this.monitorLogs(n));
  }

  renderRow(item: any) {
    return this.columns.map((n) => {
      const realCol = n.trim();
      if (!item[realCol]) { return <td />; }
      switch (realCol) {
        case "process":
          return <td>{item[realCol].split(".").pop()}</td>;
        case "sessionTime":
          return <td>{new Date(item[realCol]).toISOString().slice(11, -1)}</td>;
        default:
          return <td>{item[realCol].toString()}</td>;
      }
    });
  }

  renderTable() {
    return this.items.length > 0 ? (
      <Table className="log-table">
        <thead>
          {this.columns.map((n, index) => (
            <th key={index}>{n}</th>
          ))}
        </thead>
        <tbody>
          {this.items.map((n, index) => (
            <tr key={index} className="log-row">{this.renderRow(n)}</tr>
          ))}
        </tbody>
      </Table>
    ) : (
      this.renderEmptyContainer()
    );
  }

  renderEmptyContainer() {
    return (
      <div className="h-100 d-flex justify-content-center align-items-center">
        <CardText>No Processes Logged...</CardText>
      </div>
    );
  }

  render() {
    return (
      <BaseCard layoutCard={this.props.layoutCard} body={this.renderTable()} />
    );
  }
}

export default ProcessViewerCard;
