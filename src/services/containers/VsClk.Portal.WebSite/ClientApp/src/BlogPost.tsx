import React, { useEffect } from 'react';
import { Redirect } from 'react-router-dom';
export function BlogPost() {
    if (process.env.NODE_ENV === 'development') {
        return <Redirect to='/welcome' />;
    }
    useEffect(() => {
        window.location.replace(
            'https://devblogs.microsoft.com/visualstudio/intelligent-productivity-and-collaboration-from-anywhere/'
        );
    });
    return <></>;
}
