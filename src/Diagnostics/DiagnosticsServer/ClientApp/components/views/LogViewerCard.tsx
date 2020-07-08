import { JsonItemModal } from "@Components/modals/JsonItemModal";
import { faPause, faPlay, faTrash } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import Hub from "hub";
import { cloneDeep, uniq } from "lodash";
import { observable } from "mobx";
import { inject, observer } from "mobx-react";
import React from "react";
import {
  Badge,
  Button,
  ButtonGroup, 
  CardText,
  FormGroup,
  Input, 
  Label,
  PopoverBody, 
  Table,
  UncontrolledPopover
} from "reactstrap";
import { v4 as uuidv4 } from "uuid";
import Actions from "../../actions";
import { AppState, Filter, GridCard } from "../../appState";
import BaseCard from "./BaseCard";

@inject("hub")
@inject("appState")
@observer
class LogViewerCard extends React.Component<{
  layoutCard: any;
  appState?: AppState;
  hub?: Hub;
}> {
  @observable totalNumber: number = 0;
  @observable items: any[] = [];
  @observable isEnabled: boolean = true;
  @observable newFilter: Filter = new Filter();

  addFilterId: string = `add-filter-${uuidv4()}`;
  card: GridCard = this.props.layoutCard.card;
  logModal: React.RefObject<JsonItemModal>;

  constructor(props) {
    super(props);
    this.logModal = React.createRef<JsonItemModal>();

    // Hooks into 'newJsonLogEntry' event. When a new item gets logged,
    // the callback will be invoked as a generic 'item'. Inspect that
    // and you can get JSON from the logs.
    this.props.hub.AddJsonHubCallback((n) => this.monitorLogs(n));
  }

  componentWillUnmount() {
    this.props.hub.RemoveJsonHubCallback((n) => this.monitorLogs(n));
  }

  updateProperty(key) {
    this.card.options[key.target.name] = key.target.value;
    this.props.appState.Save();
  }

  showLog(item) {
    this.logModal.current.open(cloneDeep(item));
  }

  updateFiltersProperty(key) {
    this.card.options.filterList[key.target.name].value = key.target.value;
    // TODO: Cheap hack since every item in filter list should be observed by
    // the options observable, but it's not...
    this.props.appState.Save();
  }

  addNewFilter() {
    const filter = cloneDeep(this.newFilter);
    // TODO: Cheap hack to avoid having to implement 'PopOver' (and the needed isOpen, Enabled settings)
    // If we click the add button again, it'll take focus on it, closing the popover.
    document.getElementById(this.addFilterId).click();
    this.newFilter = new Filter();
    if (!filter.name) { filter.name = filter.key; }
      this.card.options.filterList.push(filter);

    // TODO: Cheap hack since every item in filter list should be observed by
    // the options observable, but it's not...
    this.props.appState.Save();
  }

  removeFilter(x) {
    this.card.options.filterList.remove(x);
    // TODO: Cheap hack since every item in filter list should be observed by
    // the options observable, but it's not...
    this.props.appState.Save();
  }

  monitorLogs(item: any) {
    if (!this.isEnabled) { return; }
    this.totalNumber = this.totalNumber + 1;
    if (!this.filterItem(item)) { return; }
    if (this.card.options.groupBy && this.card.options.groupBy.length > 0) {
      Actions.groupBy(this.card.options.groupBy, item, this.items);
    }
    else {
      this.items.unshift(item);
      if (this.items.length >= 100) { this.items.pop(); }
    }
  }

  filterItem(item: any) {
    if (this.card.options.groupBy && this.card.options.groupBy.length > 0) {
      if (!item[this.card.options.groupBy]) {
        return false;
      }
    }
    for (const filterOption of this.card.options.filterList) {
      if (filterOption.value.length <= 0) {
        continue;
      }
      const filters = filterOption.value.split(",");
      const includeFilters = filters.filter(f => !f.startsWith('-'));
      const excludeFilters = filters.filter(f => f.startsWith('-')).map(f => f.substr(1));
      if (includeFilters.length > 0 && !includeFilters.includes(item[filterOption.key])) {
        return false;
      }
      if (excludeFilters.length > 0 && excludeFilters.includes(item[filterOption.key])) {
        return false;
      }
    }
    return true;
  }

  getColumns() {
    let columnSetup = this.card.options.columnSettings.split(",");
    if (columnSetup.includes("*")) {
      columnSetup.splice(columnSetup.indexOf("*"), 1);
      if (this.items.length > 0) {
        const cols = Object.keys(this.items[0]);
        columnSetup = columnSetup.concat(cols);
      }
    }
    return uniq(columnSetup);
  }

  renderLevel(level: string) {
    return (
      <td>
        <Badge color={level}>{level}</Badge>
      </td>
    );
  }

  renderRow(item: any) {
    const columnSetup = this.getColumns();
    return columnSetup.map((n) => {
      const realCol = n.trim();
      if (!item[realCol]) { return <td/>; }
      switch (realCol) {
        case "level":
          return this.renderLevel(item.level);
        default:
          return <td>{item[realCol]}</td>;
      }
    });
  }

  renderEmptyContainer() {
    return (
      <div className="h-100 d-flex justify-content-center align-items-center">
        <CardText>No Items Logged...</CardText>
      </div>
    );
  }

  renderPlayPauseButton() {
    return (
      <Button size="sm" onClick={() => (this.isEnabled = !this.isEnabled)}>
        <FontAwesomeIcon icon={this.isEnabled ? faPause : faPlay} />
      </Button>
    );
  }

  renderTable() {
    const realItems = this.items.filter((n) => this.filterItem(n));
    const table =
      realItems.length > 0 ? (
        <Table className="log-table">
          <thead>
            {this.getColumns().map((n, index) => (
              <th key={`td${index}`} >{n}</th>
            ))}
          </thead>
          <tbody>
            {realItems.map((n, index) => (
              <tr key={`tr${index}`} className="log-row" onClick={() => this.showLog(n)}>
                {this.renderRow(n)}
              </tr>
            ))}
          </tbody>
        </Table>
      ) : (
        this.renderEmptyContainer()
      );
    return table;
  }

  renderFilters() {
    return this.card.options.filterList.map((n, index) => {
      return (
        <FormGroup key={index}>
          <div>
            <Label className="form-group-label-button">{n.name}</Label>
            <ButtonGroup className="toolbar-button">
              {this.renderDeleteFilterButton(n)}
            </ButtonGroup>
          </div>
          <Input
            type="text"
            placeholder={n.placeHolder}
            name={`${index}.${index}`}
            value={n.value}
            onChange={(x) => this.updateFiltersProperty(x)}
          />
        </FormGroup>
      );
    });
  }

  renderDeleteFilterButton(x: any) {
    return (
      <Button size="sm" color="danger" onClick={() => this.removeFilter(x)}>
        <FontAwesomeIcon icon={faTrash} />
      </Button>
    );
  }

  renderNewFiltersButton() {
    return (
      <FormGroup>
        <Button block={true} id={this.addFilterId}>
          Add Filter
        </Button>
        <UncontrolledPopover
          trigger="legacy"
          placement="bottom"
          target={this.addFilterId}
        >
          <PopoverBody>
            <FormGroup>
              <Label>Field</Label>
              <Input
                type="text"
                name="key"
                placeholder="JSON Object Key: Ex. 'level', 'msg'"
                value={this.newFilter.key}
                onChange={(x) =>
                  (this.newFilter[x.target.name] = x.target.value)
                }
              />
            </FormGroup>
            <Button
              disabled={this.newFilter.key.length <= 0}
              onClick={() => this.addNewFilter()}
              block={true}
            >
              Add
            </Button>
          </PopoverBody>
        </UncontrolledPopover>
      </FormGroup>
    );
  }

  renderSettings() {
    return (
      <div>
        <FormGroup>
          <Label>Columns</Label>
          <Input
            type="text"
            name="columnSettings"
            value={this.card.options.columnSettings}
            onChange={(x) => {
              this.updateProperty(x);
            }}
          />
        </FormGroup>
        <FormGroup>
          <Label>Group By</Label>
          <Input
            type="text"
            name="groupBy"
            value={this.card.options.groupBy}
            onChange={(x) => {
              this.updateProperty(x);
            }}
          />
        </FormGroup>
        {this.renderFilters()}
        {this.renderNewFiltersButton()}
      </div>
    );
  }

  render() {
    return (
      <div>
        <BaseCard
          layoutCard={this.props.layoutCard}
          settings={this.renderSettings()}
          extraButtons={this.renderPlayPauseButton()}
          body={this.renderTable()}
          footer={<div>Total Number Logged: {this.totalNumber}</div>}
        />
        <JsonItemModal ref={this.logModal} />
      </div>
    );
  }
}

export default LogViewerCard;
