import { inject, observer } from "mobx-react";
import React from "react";
import { Route, RouteComponentProps, withRouter } from "react-router";
import { AppState } from "../appState";
import "./App.css";
import Layout from "./Layout";
import LogGrid from "./views/LogGrid";

type PathParamsType = {
    param1: string,
}

// Your component own properties
type PropsType = RouteComponentProps<PathParamsType> & {
    appState?: AppState,
}

@inject("appState") @observer
class App extends React.Component<PropsType> {
    render() {
        return <Layout>
            <Route exact={true} path="/" component={LogGrid} />
            <Route exact={true} path="/logs" component={LogGrid} />
        </Layout>
    }
}

export default withRouter(App);