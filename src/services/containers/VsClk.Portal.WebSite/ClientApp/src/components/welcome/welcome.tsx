import React, { Component } from 'react';
import './welcome.css';

import { Link } from 'react-router-dom';

import { ICloudEnvironment } from '../../interfaces/cloudenvironment';

import envRegService from '../../services/envRegService';
import { AuthService } from '../../services/authService';

export interface WelcomeProps {
    onNewEnvironment: () => void;
}

export interface WelcomeState {
    environments?: ICloudEnvironment[];
}

export class Welcome extends Component<WelcomeProps, WelcomeState> {

    constructor(props: WelcomeProps) {
        super(props);
        this.state = {
        }

        AuthService.Instance.getUser().then((user) => {
            if (user) {
                envRegService.fetchEnvironments().then((environments) => {
                    this.setState({ environments });
                });
            }
        });
    }

    handleNewEnvironment = () => {
        const { onNewEnvironment } = this.props;
        onNewEnvironment();
    }

    render() {
        const { environments } = this.state;

        const MAX_RECENT = 5; // max number of environments to show in the recent list

        return (
            <div className='welcomePageContainer'>
                <div className='welcomePage'>
                    <div className='title'>
                        <h1 className='caption'>Visual Studio Online</h1>
                        <p className='subtitle detail'>Editing reimagined</p>
                    </div>
                    {environments ?
                        <div className='row'>
                            <div className='splash'>
                                <div className='section start'>
                                    <h2 className='caption'>Start</h2>
                                    <ul>
                                        <li><a role='button' href='#' onClick={this.handleNewEnvironment} title='New Environment'>New Environment</a></li>
                                    </ul>
                                </div>
                                <div className='section recent'>
                                    <h2 className='caption'>Recent</h2>
                                    {environments ?
                                        <ul className='list'>
                                            {environments.slice(0, MAX_RECENT).map(environment =>
                                                <li key={environment.id}>
                                                    <Link to={`/environment/${environment.id}`} title={`${environment.friendlyName}`}
                                                        aria-label={`Open environment ${environment.friendlyName}`}>{environment.friendlyName}</Link>
                                                    <span className='path detail' title={`${environment.friendlyName}`}>{environment.id}</span>
                                                </li>
                                            )}
                                        </ul> : undefined}
                                    {environments ? <p className={`detail ${environments.length ? 'none' : ''}`}>No recent environments</p> : undefined}
                                </div>
                                <div className='section help'>
                                    <h2 className='caption'>Help</h2>
                                    <ul>
                                        <li className='keybindingsReferenceLink'><a href='command:workbench.action.keybindingsReference'>Printable keyboard cheatsheet</a></li>
                                        <li><a href='command:workbench.action.openIntroductoryVideosUrl'>Introductory videos</a></li>
                                        <li><a href='command:workbench.action.openTipsAndTricksUrl'>Tips and Tricks</a></li>
                                        <li><a href='command:workbench.action.openDocumentationUrl'>Product documentation</a></li>
                                        <li><a href='https://stackoverflow.com/questions/tagged/vsweb?sort=votes&amp;pageSize=50'>Stack Overflow</a></li>
                                    </ul>
                                </div>
                            </div>
                            <div className='commands'>
                                <div className='section customize'>
                                    <h2 className='caption'>Customize</h2>
                                    <div className='list'>
                                        <div className='item showLanguageExtensions'><button role='group' data-href='command:workbench.extensions.action.showLanguageExtensions'><h3 className='caption'>Tools and languages</h3> <span className='detail'>Install support for <span className='extensionPackList'><a title='Install additional support for JavaScript' className='installExtension' data-extension='dbaeumer.vscode-eslint' href='javascript:void(0)'>JavaScript</a><span title='JavaScript support is already installed' className='enabledExtension' data-extension='dbaeumer.vscode-eslint'>JavaScript</span>, <a title='Install additional support for TypeScript' className='installExtension installed' data-extension='ms-vscode.vscode-typescript-tslint-plugin' href='javascript:void(0)'>TypeScript</a><span title='TypeScript support is already installed' className='enabledExtension installed' data-extension='ms-vscode.vscode-typescript-tslint-plugin'>TypeScript</span>, <a title='Install additional support for Python' className='installExtension' data-extension='ms-python.python' href='javascript:void(0)'>Python</a><span title='Python support is already installed' className='enabledExtension' data-extension='ms-python.python'>Python</span>, <a title='Install additional support for PHP' className='installExtension' data-extension='felixfbecker.php-pack' href='javascript:void(0)'>PHP</a><span title='PHP support is already installed' className='enabledExtension' data-extension='felixfbecker.php-pack'>PHP</span>, <a title='Show Azure extensions' href='command:workbench.extensions.action.showAzureExtensions'>Azure</a>, <a title='Install additional support for Docker' className='installExtension' data-extension='peterjausovec.vscode-docker' href='javascript:void(0)'>Docker</a><span title='Docker support is already installed' className='enabledExtension' data-extension='peterjausovec.vscode-docker'>Docker</span></span> and <a href='command:workbench.extensions.action.showLanguageExtensions' title='Show more language extensions'>more</a>
                                        </span></button></div>
                                        <div className='item showRecommendedKeymapExtensions'><button role='group' data-href='command:workbench.extensions.action.showRecommendedKeymapExtensions'><h3 className='caption'>Settings and keybindings</h3> <span className='detail'>Install the settings and keyboard shortcuts of <span className='keymapList'><a title='Install Vim keymap' className='installExtension' data-extension='vscodevim.vim' href='javascript:void(0)'>Vim</a><span title='Vim keymap is already installed' className='enabledExtension' data-extension='vscodevim.vim'>Vim</span>, <a title='Install Sublime keymap' className='installExtension' data-extension='ms-vscode.sublime-keybindings' href='javascript:void(0)'>Sublime</a><span title='Sublime keymap is already installed' className='enabledExtension' data-extension='ms-vscode.sublime-keybindings'>Sublime</span>, <a title='Install Atom keymap' className='installExtension' data-extension='ms-vscode.atom-keybindings' href='javascript:void(0)'>Atom</a><span title='Atom keymap is already installed' className='enabledExtension' data-extension='ms-vscode.atom-keybindings'>Atom</span></span> and <a href='command:workbench.extensions.action.showRecommendedKeymapExtensions' title='Show other keymap extensions'>others</a>
                                        </span></button></div>
                                        <div className='item selectTheme'><button data-href='command:workbench.action.selectTheme'><h3 className='caption'>Color theme</h3> <span className='detail'>Make the editor and your code look the way you love</span></button></div>
                                    </div>
                                </div>
                                <div className='section learn'>
                                    <h2 className='caption'>Learn</h2>
                                    <div className='list'>
                                        <div className='item showCommands'><button data-href='command:workbench.action.showCommands'><h3 className='caption'>Find and run all commands</h3> <span className='detail'>Rapidly access and search commands from the Command Palette (<span className='shortcut' data-command='workbench.action.showCommands'>⇧⌘P</span>)</span></button></div>
                                        <div className='item showInterfaceOverview'><button data-href='command:workbench.action.showInterfaceOverview'><h3 className='caption'>Interface overview</h3> <span className='detail'>Get a visual overlay highlighting the major components of the UI</span></button></div>
                                    </div>
                                </div>
                            </div>
                        </div> :
                        <div className='row'>
                            <div className='splash'>
                                <div className='section start'>
                                    <h2 className='caption'>Please Login</h2>
                                </div>
                            </div>
                        </div>}
                </div>
            </div>
        );
    }
}
