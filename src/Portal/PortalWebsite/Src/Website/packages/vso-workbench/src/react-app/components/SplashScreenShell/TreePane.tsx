import React from 'react';

import { CodeLine } from './CodeLine';

export const TreePane: React.FunctionComponent<{}> = () => {
    return (<div className="tree">
        <div className="line"></div>
        <ul className="lines">
            <CodeLine percentage={60} />
            <CodeLine percentage={70} />
            <CodeLine percentage={50} />
            <CodeLine percentage={64} />
            <CodeLine percentage={83} />
            <CodeLine percentage={81} />
            <CodeLine percentage={40} />
            <CodeLine percentage={50} />
            <CodeLine percentage={68} />
            <CodeLine percentage={60} />
            <CodeLine percentage={75} />
            <CodeLine percentage={77} />
            <CodeLine percentage={40} />
        </ul>
    </div>);
};
