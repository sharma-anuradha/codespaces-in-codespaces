import LogViewerCard from "@Components/views/LogViewerCard";
import {
  faFileImport,
  faPlus,
  faRedo,
  faMinus,
  faTrash,
} from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { cloneDeep } from "lodash";
import { observable } from "mobx";
import { inject, observer } from "mobx-react";
import React from "react";
import { Responsive, WidthProvider } from "react-grid-layout";
import {
  Button,
  ButtonGroup,
  FormGroup,
  Input,
  PopoverBody,
  PopoverHeader,
  UncontrolledPopover,
  TabContent,
  Label,
  Nav,
  NavItem,
  NavLink,
} from "reactstrap";
import { v4 as uuidv4 } from "uuid";
import Hub from "hub";
import Actions from "../../actions";
import {
  AppState,
  CardType,
  DefaultSize,
  GridCard,
  CardTab,
} from "../../appState";
import BaseCard from "./BaseCard";
import IsNgrokRunningCard from "./IsNgrokRunningCard";
import ProcessViewerCard from "./ProcessViewerCard";
import classnames from "classnames";

const ResponsiveGridLayout = WidthProvider(Responsive);

@inject("hub")
@inject("appState")
@observer
class LogGrid extends React.Component<{
  appState?: AppState;
  hub?: Hub;
}> {
  @observable cardTemplates: GridCard[];
  @observable selectedCardType: GridCard;
  @observable cardJsonImport: string = "";
  @observable newCardTab: CardTab = new CardTab();

  constructor(props) {
    super(props);
    this.cardTemplates = Actions.generateNewCardsTemplate();
    this.selectedCardType = this.cardTemplates[0];
  }

  updateDropProperty(x) {
    this.selectedCardType = this.cardTemplates[x.target.value];
  }

  onLayoutChange(layouts) {
    this.props.appState.cardTabs[
      this.props.appState.selectedTab
    ].layouts = layouts;
    this.props.appState.Save();
  }

  renderCards() {
    let card: {};
    return this.props.appState.cardTabs[
      this.props.appState.selectedTab
    ].cards.map((n) => {
      switch (n.card.type) {
        case CardType.LogViewer:
          card = <LogViewerCard layoutCard={n} />;
          break;
        case CardType.IsNgrokRunning:
          card = <IsNgrokRunningCard layoutCard={n} />;
          break;
        case CardType.ProcessViewer:
          card = <ProcessViewerCard layoutCard={n} />;
          break;
        case CardType.Debug:
          card = <BaseCard layoutCard={n} />;
          break;
        default:
          throw new Error(`Unsupported Card Type: ${n}`);
      }
      return (
        <div data-grid={n} key={n.card.id}>
          {card}
        </div>
      );
    });
  }

  importCard() {
    try {
      const card = JSON.parse(this.cardJsonImport);
      this.addCard(card);
      document.getElementById("import_card").click();
      this.cardJsonImport = "";
    } catch (error) {
      // TODO: Add error handling popover to expose errors.
      console.log(error);
    }
  }

  addCard(newCard: GridCard, closeWindow: boolean = false) {
    if (newCard.type === CardType.Unknown) {
      return;
    }
    const cloneCard = cloneDeep(newCard);
    cloneCard.id = `${uuidv4()}_${cloneCard.type}`;
    const defaultSize = new DefaultSize();
    const size = cloneCard.defaultSize
      ? cloneCard.defaultSize
      : new DefaultSize();
    const card = {
      w: size.width ? size.width : defaultSize.width,
      h: size.height ? size.height : defaultSize.height,
      x: 0,
      y: 0,
      minW: size.minWidth ? size.minWidth : defaultSize.minWidth,
      minH: size.minHeight ? size.minHeight : defaultSize.minHeight,
      card: cloneCard,
    };
    this.props.appState.cardTabs[this.props.appState.selectedTab].cards.push(
      card
    );
    // TODO: Cheap hack to avoid having to implement 'PopOver' (and the needed isOpen, Enabled settings)
    // If we click the add button again, it'll take focus on it, closing the popover.
    if (closeWindow) {
      document.getElementById("add_card").click();
    }
  }

  reloadLogs() {
    this.props.hub.SendReloadLogMessage();
  }

  renderImportCard() {
    return (
      <UncontrolledPopover
        trigger="legacy"
        placement="bottom"
        target="import_card"
      >
        <PopoverHeader className="text-header">Import Card</PopoverHeader>
        <PopoverBody>
          <FormGroup>
            <Input
              type="textarea"
              name="text"
              onChange={(e) => {
                this.cardJsonImport = e.target.value;
              }}
              value={this.cardJsonImport}
            />
          </FormGroup>
          <FormGroup>
            <Button
              block={true}
              disabled={this.cardJsonImport.length <= 0}
              onClick={() => {
                this.importCard();
              }}
            >
              Import
            </Button>
          </FormGroup>
        </PopoverBody>
      </UncontrolledPopover>
    );
  }

  renderAddNewCard() {
    return (
      <UncontrolledPopover
        trigger="legacy"
        placement="bottom"
        target="add_card"
      >
        <PopoverHeader className="text-header">Add New Card</PopoverHeader>
        <PopoverBody>
          <FormGroup>
            <Input
              type="select"
              name="cardSelect"
              value={this.cardTemplates.indexOf(this.selectedCardType)}
              onChange={(x) => this.updateDropProperty(x)}
            >
              {this.cardTemplates.map((n, index) => (
                <option key={index} value={index}>
                  {n.name}
                </option>
              ))}
            </Input>
          </FormGroup>
          <FormGroup>{this.selectedCardType.description}</FormGroup>
          <FormGroup>
            <Button
              block={true}
              disabled={this.selectedCardType.type === CardType.Unknown}
              onClick={() => {
                this.addCard(this.selectedCardType, true);
              }}
            >
              Add
            </Button>
          </FormGroup>
        </PopoverBody>
      </UncontrolledPopover>
    );
  }

  renderTabs() {
    const selectedTab = this.props.appState.cardTabs[this.props.appState.selectedTab];
    if (selectedTab) {
      document.title = `Diagnostics - ${selectedTab.name}`;
    }
    return this.props.appState.cardTabs.map((n, index) => {
      return (
        <NavItem key={`tab_${index}`}>
          <NavLink
            style={{ cursor: "hand" }}
            className={classnames({
              active: this.props.appState.selectedTab === index,
            })}
            onClick={() => {
              if (this.props.appState.cardTabs[index]) {
                this.props.appState.selectedTab = index;
              }
            }}
          >
            {n.name}
            {index === this.props.appState.selectedTab ? (
              <ButtonGroup
                style={{ marginLeft: "15px" }}
                className="layout-buttons"
              >
                <Button size="sm" id="add_card">
                  <FontAwesomeIcon icon={faPlus} />
                </Button>
                {this.renderAddNewCard()}
                <Button size="sm" id="import_card">
                  <FontAwesomeIcon icon={faFileImport} />
                </Button>
                {this.renderImportCard()}
                <Button size="sm" id="reload_logs" onClick={() => this.reloadLogs()}>
                  <FontAwesomeIcon icon={faRedo} />
                </Button>
                {this.props.appState.cardTabs.length > 1 ? (
                  <Button size="sm" color="danger"
                    onClick={() => {
                      this.props.appState.cardTabs.remove(
                        this.props.appState.cardTabs[index]
                      );
                      if (index === this.props.appState.selectedTab) {
                        this.props.appState.selectedTab = 0;
                      }
                    }}
                    id={`remove_tab_${index}`}
                  >
                    <FontAwesomeIcon icon={faTrash} />
                  </Button>
                ) : (
                  <div />
                )}
              </ButtonGroup>
            ) : (
              <div />
            )}
          </NavLink>
        </NavItem>
      );
    });
  }

  renderNewTab() {
    return (
      <NavItem key={`tab_new`}>
        <NavLink>
          <Button size="sm" id="add_tab">
            <FontAwesomeIcon icon={faPlus} />
          </Button>
        </NavLink>
        <UncontrolledPopover
          trigger="legacy"
          placement="bottom"
          target="add_tab"
        >
          <PopoverBody>
            <FormGroup>
              <Label>Name</Label>
              <Input
                type="text"
                name="name"
                value={this.newCardTab.name}
                onChange={(x) => {
                  this.newCardTab.name = x.target.value;
                }}
              />
            </FormGroup>
            <Button
              onClick={() => {
                if (this.newCardTab.name === "") {
                  this.newCardTab.name = "Blank???";
                }
                document.getElementById("add_tab").click();
                this.props.appState.cardTabs.push(this.newCardTab);
                this.newCardTab = new CardTab();
                this.props.appState.selectedTab =
                  this.props.appState.cardTabs.length - 1;
              }}
              block={true}
            >
              Add Tab
            </Button>
          </PopoverBody>
        </UncontrolledPopover>
      </NavItem>
    );
  }

  render() {
    return (
      <div className="layout">
        <Nav tabs={true}>
          {this.renderTabs()}
          {this.renderNewTab()}
        </Nav>
        <TabContent style={{ marginTop: "10px" }}>
          <ResponsiveGridLayout
            className="layout"
            layouts={
              this.props.appState.cardTabs[this.props.appState.selectedTab]
                .layouts
            }
            onLayoutChange={(layout, layouts) => this.onLayoutChange(layouts)}
            breakpoints={{ lg: 1200, md: 996, sm: 768, xs: 480, xxs: 0 }}
            cols={{ lg: 12, md: 10, sm: 6, xs: 4, xxs: 2 }}
          >
            {this.renderCards()}
          </ResponsiveGridLayout>
        </TabContent>
      </div>
    );
  }
}

export default LogGrid;
