import { observable } from "mobx";
import { observer } from "mobx-react";
import * as React from "react";
import ReactJson from "react-json-view";
import { Modal, ModalBody, ModalFooter, ModalHeader } from "reactstrap";

@observer
export class JsonItemModal extends React.Component<{ }, {}> {
    state: any;
    item: any = {};

    @observable isLoading: boolean;

    constructor(props: any) {
        super(props);
        this.state = {
            modal: false
        };
        this.toggle = this.toggle.bind(this);
    }

    async open(item) {
        this.item = item;
        this.toggle();
    }

    toggle() {
        this.setState({
            modal: !this.state.modal
        });
    }

    render() {
        return (
            <div className="group community-container">
                <Modal size="lg" scrollable={true} isOpen={this.state.modal} toggle={this.toggle}>
                    <ModalHeader toggle={this.toggle}>
                        <h4>Log Viewer</h4>
                    </ModalHeader>
                    <ModalBody>
                        <ReactJson src={this.item} theme="monokai" />
                    </ModalBody>
                    <ModalFooter/>
                </Modal>
            </div>
        );
    }
}