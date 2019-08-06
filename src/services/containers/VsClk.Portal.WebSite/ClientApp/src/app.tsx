import React, { Component } from 'react';
import { Route } from 'react-router';
import './app.css';

import { AppContextInterface, AppContextProvider } from './appContext';

import { Main } from './components/main/main';
import { Welcome } from './components/welcome/welcome';
import { Workbench } from './components/workbench/workbench';
import { Loader } from './components/loader/loader';

export interface AppState {
  isLoading: boolean;
}

export class App extends Component<{}, AppState> {

  constructor(props: any) {
    super(props);

    this.state = {
      isLoading: true
    }
  }

  async componentDidMount() {
    this.setState({
      isLoading: false
    });
  }

  private renderMain() {
    const appContext: AppContextInterface = { name: '' };

    return (
      <AppContextProvider value={appContext}>
        <Route exact path='/' render={() => {
          location.href = 'https://devblogs.microsoft.com/visualstudio/intelligent-productivity-and-collaboration-from-anywhere/';
          return null;
        }}  />
        <Route exact path='/environments' component={Main} />
        <Route exact path='/welcome' component={Welcome} />
        <Route path='/environment/:id' component={Workbench} />
      </AppContextProvider>
      );
  }

  render() {
    const { isLoading } = this.state;

    return (
      <div className='vssass' key='main-app'>
        {
          (isLoading)
            ? <Loader message='Signing in...' />
            : this.renderMain()
        }
      </div>

    );
  }
}