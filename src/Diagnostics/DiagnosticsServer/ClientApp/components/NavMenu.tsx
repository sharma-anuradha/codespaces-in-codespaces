import { inject, observer } from "mobx-react";
import * as React from "react";
import { Link } from "react-router-dom";
import { Collapse, CustomInput, Nav, Navbar, NavbarBrand, NavbarToggler, NavItem } from "reactstrap";
import actions from "../actions";
import { AppState } from "../appState";

@inject("appState") @observer
class NavMenu extends React.Component<{ appState?: AppState }, {}> {
    state: any;
    constructor(props: any) {
        super(props);

        this.toggle = this.toggle.bind(this);
        this.state = {
            isOpen: false,
            checked: actions.getLocalTheme() === "bootstrap-dark"
        };
    }
    toggle() {
        this.setState({
            isOpen: !this.state.isOpen
        });
    }
    setDarkMode (checked: boolean) {
        this.setState({ checked });
        actions.setLocalTheme(checked ? "bootstrap-dark" : "bootstrap");
    }
    render() {
        return (<Navbar className="navbar navbar-dark bg-dark navbar-expand-md justify-content-between fixed-top" expand="md">
            <div className="container-fluid">
                <NavbarBrand><Link to="/">Codespaces Diagnostics</Link></NavbarBrand>
                <NavbarToggler onClick={this.toggle} />
                <Collapse isOpen={this.state.isOpen} navbar={true}>
                    <Nav className="dual-nav w-50 order-1 order-md-0" navbar={true}>
                        <NavItem>
                            <Link className="nav-link" to="/logs">Logs</Link>
                        </NavItem>
                        <div className="nav-link">
                            <CustomInput checked={this.state.checked} 
                            onChange={e => this.setDarkMode(e.target.checked)}
                            type="switch" id="exampleCustomSwitch" name="customSwitch" label="Dark Mode" />
                        </div>
                    </Nav>
                </Collapse>
            </div>
        </Navbar>);
    }
}

export default NavMenu;