import React, { Component } from 'react';
import { Route } from 'react-router';
import './app.css';

import { AuthService } from './services/authService';

import { AppContextInterface, AppContextProvider } from './appContext';

import { Main } from './components/main/main';
import { Workbench } from './components/workbench/workbench';
import { Loader } from './components/loader/loader';

export interface AppState {
  loading: boolean;
}

export class App extends Component<{}, AppState> {

  constructor(props: any) {
    super(props);

    this.state = {
      loading: true
    }
  }

  async componentDidMount() {
    await AuthService.Instance.init();
    const isAuthenticated = await AuthService.Instance.isAuthenticated();
    if (!isAuthenticated) {
      window.location.href = 'https://aka.ms/vsfutures';
    } else {
      this.setState({ loading: false })
    }
  }

  render() {
    const { loading } = this.state;
    const appContext: AppContextInterface = {
      name: ''
    };

    return (
      <div className='vssass'>
        {!loading ?
            <AppContextProvider value={appContext}>
              <Route exact path='/' component={Main} />
              <Route path='/environment' component={Workbench} />
              <Route path='/environment/:id' component={Workbench} />
            </AppContextProvider>
            : <Loader mainMessage='' />}
      </div>

    );
  }
}