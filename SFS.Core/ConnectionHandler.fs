namespace SFS.Core

module ConnectionHandler =
        
    open SFS.Core.Http
    open SFS.Core.Logging
    open SFS.Core.Routing    
    
    let handle (routes: Map<string,Route>) (notFound: Route) (logger:Logger) (request:Request) =
        
        // Match the route.
        let route = matchRoute routes notFound request.route
        
        // Create the headers.
        
        {
            code = ok
            headers = Map.empty
            version = "1.1"
            body = None
        }