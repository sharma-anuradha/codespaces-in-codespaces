import { initHostingHtmlTags as initHostingHtmlTagsVSO } from './split/vso/initHostingHtmlTags';
import { initHostingHtmlTags as initHostingHtmlTagsGithub } from './split/github/initHostingHtmlTagsGithub';
import { isHostedOnGithub } from 'vso-client-core';

export const initHostingHtmlTags = async () => {
    (!isHostedOnGithub())
        ? await initHostingHtmlTagsVSO()
        : await initHostingHtmlTagsGithub();
};
