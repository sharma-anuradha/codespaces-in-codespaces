import * as React from 'react';

export const connect = <T, K, X = Partial<T>>(
    getProps: (props: X) => T,
    Component: React.ComponentType<T>
) => {
    return (props: X) => {
        const extendedProps = getProps(props);

        return <Component {...extendedProps} />;
    };
};
