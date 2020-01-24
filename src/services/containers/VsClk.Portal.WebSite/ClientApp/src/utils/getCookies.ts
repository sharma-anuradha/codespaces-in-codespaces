interface ICookie {
    name: string;
    value: string;
}

export const getAllCookies = (): ICookie[] => {
    const pairs = document.cookie.split(';');
    const cookies = [];
    for (let i = 0; i < pairs.length; i++){
      const pair = pairs[i].split("=");
      const cookie: ICookie = {
          name: (pair[0]+'').trim(),
          value: unescape(pair.slice(1).join('='))
      };
      
      cookies.push(cookie);
    }

    return cookies;
}
