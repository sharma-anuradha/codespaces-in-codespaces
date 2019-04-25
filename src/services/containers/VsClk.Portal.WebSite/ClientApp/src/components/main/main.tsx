import React, { Component } from 'react';
import { RouteComponentProps } from 'react-router';

import './main.css';
import './monaco.css';

import vscodemodules from './vscodemodules';

import { Welcome } from '../welcome/welcome';
import { StatusBar } from '../statusbar/statusbar';
import { TitleBar } from '../titlebar/titlebar';
import { ActivityBar } from '../activitybar/activitybar';
import { PromptModal } from '../dialogs/prompt/prompt';
import { Loader } from '../loader/loader';

import { loader } from '../../loader';
import envRegService from '../../services/envRegService';


interface MainProps extends RouteComponentProps {
}

interface MainState {
    loading?: boolean;
    showNameModal?: boolean;
}

export class Main extends Component<MainProps, MainState> {

    constructor(props: any) {
        super(props);

        this.state = {
            loading: true
        };
    }

    async componentWillMount() {
        await this.initLoadAsync();
        this.lazyLoadVSCode();
    }

    initLoadAsync() {
        // Load required CSS resources from vscode.
        return loader.loadModules(vscodemodules.initialCSSModules)
            .then(() => {
                this.setState({ loading: false });
            })
    }

    lazyLoadVSCode() {
        return loader.loadModules(vscodemodules.lazyLoadModules);
    }

    createNewEnvironment = () => {
        this.setState({ showNameModal: true });
    }

    onNewEnvironmentNameSelected = (name: string) => {
        // show modal to get name.
        envRegService.newEnvironment(name).then(environment => {
            // Route to the new environment id.
            this.props.history.push(`/environment/${environment.id}`)
        })
    }

    render() {
        const { loading } = this.state;
        if (loading) return <Loader mainMessage='Loading...'/>

        return (
            <div>
                <div className='monaco-workbench web'>
                    <ActivityBar position='left' />
                    <div className='part editor' style={{ width: 'calc(100% - 50px)', height: 'calc(100% - 52px)', top: '30px', bottom: '22px', left: '50px', position: 'absolute' }}>
                        <div className='content' style={{ position: 'relative', height: '100%', left: '0', right: '0', margin: 'auto', overflow: 'auto' }}>
                            <div className='editor-group-container' style={{ height: '100%' }}>
                                <Welcome onNewEnvironment={this.createNewEnvironment} />
                            </div>
                        </div>
                    </div>
                    <StatusBar />
                    <TitleBar />
                </div>
                <PromptModal
                    open={this.state.showNameModal}
                    onClose={() => this.setState({ showNameModal: false })}
                    placeholder={'Choose a name for your environment'} onPrompt={this.onNewEnvironmentNameSelected} />
            </div>
        );
    }
}