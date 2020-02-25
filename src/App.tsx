import React from 'react';
import { HashRouter, Switch, Route } from 'react-router-dom';
import { PAGE_PATHS } from './constants';
import Calendar from './pages/Calendar';
import Main from './pages/Main';
import { SWRConfig } from 'swr';
import axios from 'axios';

export default () => (
  <SWRConfig
    value={{
      fetcher: url => axios.get(url).then(r => r.data),
    }}
  >
    <HashRouter>
      <Switch>
        <Route path={`${PAGE_PATHS.CALENDAR}/:id`} component={Calendar} />
        <Route path="/" component={Main} />
      </Switch>
    </HashRouter>
  </SWRConfig>
);
