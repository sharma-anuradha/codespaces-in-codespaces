import React, { FC } from 'react';

import { Image } from 'office-ui-fabric-react';

import image from './everywhere.svg';

export const EverywhereImage: FC<{ className?: string }> = ({ className }) => {
    return <Image src={image} width={326} height={193} className={className} alt='VS Online' />;
};
