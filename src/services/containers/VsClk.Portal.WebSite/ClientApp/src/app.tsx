import React, { Component } from 'react';
import { Route } from 'react-router';
import './app.css';

import { AuthService } from './services/authService';

import { AppContextInterface, AppContextProvider } from './appContext';

import { Main } from './components/main/main';
import { Workbench } from './components/workbench/workbench';

export interface AppState {

}

export class App extends Component<{}, AppState> {

  constructor(props: any) {
    super(props);

    this.state = {
    }

    AuthService.Instance.init();
  }

  render() {
    const appContext: AppContextInterface = {
      name: ''
    };

    return (
      <div className='vssass'>
        <AppContextProvider value={appContext}>
          <Route exact path='/' component={Main} />
          <Route path='/environment' component={Workbench} />
          <Route path='/environment/:id' component={Workbench} />
        </AppContextProvider>
      </div>
    );
  }
}