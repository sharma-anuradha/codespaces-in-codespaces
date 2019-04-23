import React, { Component } from 'react';
import { RouteComponentProps } from 'react-router';
import './workbench.css';

import { Loader } from '../loader/loader';

import { loader } from '../../loader';
import envRegService from '../../services/envRegService';

export interface WorkbenchProps extends RouteComponentProps {
}

export interface WorkbenchState {
    isLoading?: boolean;
    friendlyName?: string;
}

export class Workbench extends Component<WorkbenchProps, WorkbenchState> {

    private id: string;

    constructor(props: WorkbenchProps) {
        super(props);

        this.state = {
            isLoading: true
        }
        const { match: { params }, history } = this.props;
        this.id = (params as any).id;

        history.listen((location, action) => {
            // location is an object like window.location
            Array.from(document.getElementsByClassName('monaco-aria-container')).forEach((el) => {
                el.parentNode.removeChild(el);
            });
            Array.from(document.querySelectorAll('body>.monaco-workbench')).forEach((el) => {
                el.parentNode.removeChild(el);
            });
            document.body.className = 'monaco-shell vs-dark';
            console.log(action, location.pathname, location.state);
        });
    }

    componentDidMount() {
        if (this.state.isLoading) {
            const promises = [];
            // Check if VSCode is loaded.
            promises.push(loader.loadWorkbench());
            if (this.id) promises.push(
                envRegService.getEnvironment(this.id)
                    .then((environment) => {
                        this.setState({ friendlyName: environment.friendlyName });
                    }));
            Promise.all(promises).then(() => {
                this.setState({ isLoading: false });
            })
        }
    }

    render() {
        const { isLoading, friendlyName } = this.state;

        return (
            <div>
                {isLoading ? <Loader mainMessage={`Loading Workbench ${friendlyName || ''}`} /> :
                    <div></div>
                }
            </div>
        );
    }
}
