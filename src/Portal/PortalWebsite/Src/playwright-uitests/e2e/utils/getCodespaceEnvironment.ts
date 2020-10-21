async function getCodespaceEnvironment(env:any) {
  switch(env) {
      case 'dev':
          return 'development';       
      case 'ppe':   
          return 'pre-production';
      case 'prod':
          return'production';
      default:                      
          return 'development';
    }
}

module.exports = {getCodespaceEnvironment};