import React, { Component } from 'react';
import { RouteComponentProps } from 'react-router';
import './workbench.css';

import { Loader } from '../loader/loader';

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
    }

    finishLoading = () => {
        this.setState({ isLoading: false });
    }

    render() {
        const { isLoading, friendlyName } = this.state;

        const iframeStyles = {
            width: '100%',
            height: '100%',
            border: '0',
            position: 'absolute',
            left: '0',
            right: '0'
        } as React.CSSProperties;

        const port = parseInt(localStorage.getItem('vsonline.port'), 10);
        const appUrl = `https://localhost:${port || '8000'}`;

        return (
            <div>
                {
                    (isLoading)
                        ? <Loader message={`Loading VS Online...`} />
                        : null
                }
                <iframe
                    style={iframeStyles}
                    src={appUrl}
                    onLoad={this.finishLoading} />
            </div>
        );
    }
}
