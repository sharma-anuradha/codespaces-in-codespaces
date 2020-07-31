import { JsonItemModal } from "@Components/modals/JsonItemModal";
import { faCog, faFileExport, faTrash } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { cloneDeep } from "lodash";
import { inject, observer } from "mobx-react";
import React from "react";
import {
  Button,
  ButtonGroup,
  Card,
  CardBody,
  CardFooter,
  CardHeader,
  FormGroup,
  Input,
  Label,
  PopoverBody,
  PopoverHeader,
  UncontrolledPopover
} from "reactstrap";
import { v4 as uuidv4 } from "uuid";
import { AppState, GridCard } from "../../appState";

@inject("appState")
@observer
class BaseCard extends React.Component<{
  layoutCard: any;
  appState?: AppState;
  settings?: any;
  extraButtons?: any[];
  body?: any;
  footer?: any;
}> {
  card: GridCard = this.props.layoutCard.card;
  disposeCardObserve: any;
  disposeCardOptionsObserve: any;
  settingId: string = `settings-${uuidv4()}`;
  jsonModal: React.RefObject<JsonItemModal>;

  constructor(props) {
    super(props);
    this.jsonModal = React.createRef<JsonItemModal>();
    // Observe returns a 'disposer' function, which will run when we unmount the component.
    this.disposeCardObserve = this.props.appState.Observe(
      this.props.layoutCard
    );
    this.disposeCardOptionsObserve = this.props.appState.Observe(this.card);
  }

  showJson() {
    this.jsonModal.current.open(cloneDeep(this.props.layoutCard.card));
  }

  renderDeleteButton() {
    return (
      <Button
        size="sm"
        color="danger"
        onClick={() => this.props.appState.cardTabs[this.props.appState.selectedTab].cards.remove(this.props.layoutCard)}
      >
        <FontAwesomeIcon icon={faTrash} />
      </Button>
    );
  }

  renderExportButton() {
    return (
      <Button size="sm" onClick={() => this.showJson()} type="button">
        <FontAwesomeIcon icon={faFileExport} />
      </Button>
    );
  }

  renderSettingsButton() {
    return (
      <Button size="sm" id={this.settingId} type="button">
        <FontAwesomeIcon icon={faCog} />
      </Button>
    );
  }

  renderSettingsBody() {
    return (
      <UncontrolledPopover
        className="log-popover-container"
        placement="bottom"
        target={this.settingId}
      >
        <PopoverHeader className="text-header">
          {this.card.name} Settings
        </PopoverHeader>
        <PopoverBody>
          <FormGroup>
            <Label>Name</Label>
            <Input
              type="text"
              name="name"
              value={this.card.name}
              onChange={(x) => {
                this.card[x.target.name] = x.target.value;
              }}
            />
          </FormGroup>
          {this.props.settings}
        </PopoverBody>
      </UncontrolledPopover>
    );
  }

  componentWillUnmount() {
    this.disposeCardObserve();
    this.disposeCardOptionsObserve();
  }

  render() {
    return (
      <div>
        <Card>
          <CardHeader>
            {this.card.name}
            <ButtonGroup className="toolbar-button">
              {this.renderDeleteButton()}
              {(this.props.extraButtons || []).map(x => x)}
              {this.renderExportButton()}
              {this.renderSettingsButton()}
            </ButtonGroup>
            {this.renderSettingsBody()}
          </CardHeader>
          <CardBody className="table-card">
            {this.props.body || <div />}
          </CardBody>
          <CardFooter>{this.props.footer || <div />}</CardFooter>
        </Card>
        <JsonItemModal ref={this.jsonModal} />
      </div>
    );
  }
}

export default BaseCard;
