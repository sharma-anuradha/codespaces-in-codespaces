import LogViewerCard from "@Components/views/LogViewerCard";
import { faFileImport, faPlus } from "@fortawesome/free-solid-svg-icons";
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
  UncontrolledPopover
} from "reactstrap";
import { v4 as uuidv4 } from "uuid";
import Actions from "../../actions";
import { AppState, CardType, DefaultSize, GridCard } from "../../appState";
import BaseCard from "./BaseCard";
import IsNgrokRunningCard from "./IsNgrokRunningCard";
import ProcessViewerCard from "./ProcessViewerCard";
const ResponsiveGridLayout = WidthProvider(Responsive);

@inject("appState")
@observer
class LogGrid extends React.Component<{ appState?: AppState }> {
  @observable cardTemplates: GridCard[];
  @observable layouts: any;
  @observable selectedCardType: GridCard;
  @observable cardJsonImport: string = "";

  constructor(props) {
    super(props);
    this.layouts = this.props.appState.layouts;
    this.cardTemplates = Actions.generateNewCardsTemplate();
    this.selectedCardType = this.cardTemplates[0];
  }

  updateDropProperty(x) {
    this.selectedCardType = this.cardTemplates[x.target.value];
  }

  onLayoutChange(layouts) {
    this.layouts = layouts;
    Actions.saveToLocalStorage("log_layouts", layouts);
  }

  renderCards() {
    let card: {};
    return this.props.appState.cards.map((n) => {
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
    if (newCard.type === CardType.Unknown) { return; }
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
    this.props.appState.cards.push(card);
    // TODO: Cheap hack to avoid having to implement 'PopOver' (and the needed isOpen, Enabled settings)
    // If we click the add button again, it'll take focus on it, closing the popover.
    if (closeWindow) {
      document.getElementById("add_card").click();
    }
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
            <Input type="textarea" name="text" onChange={(e) => {this.cardJsonImport = e.target.value}} value={this.cardJsonImport} />
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

  render() {
    return (
      <div className="layout">
        <ButtonGroup className="layout-buttons">
          <Button id="add_card">
            <FontAwesomeIcon icon={faPlus} />
          </Button>
          <Button id="import_card">
            <FontAwesomeIcon icon={faFileImport} />
          </Button>
        </ButtonGroup>
        {this.renderAddNewCard()}
        {this.renderImportCard()}
        <ResponsiveGridLayout
          className="layout"
          layouts={this.layouts}
          onLayoutChange={(layout, layouts) =>
            this.onLayoutChange(layouts)
          }
          breakpoints={{ lg: 1200, md: 996, sm: 768, xs: 480, xxs: 0 }}
          cols={{ lg: 12, md: 10, sm: 6, xs: 4, xxs: 2 }}
        >
          {this.renderCards()}
        </ResponsiveGridLayout>
      </div>
    );
  }
}

export default LogGrid;
