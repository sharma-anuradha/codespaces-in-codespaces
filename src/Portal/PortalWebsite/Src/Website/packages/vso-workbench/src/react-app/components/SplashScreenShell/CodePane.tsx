import React from 'react';
import { CodeLine } from './CodeLine';

export const CodePane: React.FunctionComponent<{}> = () => {
    return (<div className="code">
        <ul className="lines">
            <CodeLine percentage={30} />
            <CodeLine percentage={70} />
            <CodeLine percentage={50} />
            <CodeLine percentage={25} />
            <CodeLine percentage={0} />
            <CodeLine percentage={15} />
            <CodeLine percentage={40} />
            <CodeLine percentage={55} />
            <CodeLine percentage={15} />
            <CodeLine percentage={0} />
            <CodeLine percentage={60} />
            <CodeLine percentage={75} />
            <CodeLine percentage={77} />
            <CodeLine percentage={40} />
        </ul>
    </div>);
};
