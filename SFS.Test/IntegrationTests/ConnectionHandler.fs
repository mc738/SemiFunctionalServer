namespace SFS.Test.IntegrationTests

open Microsoft.VisualStudio.TestTools.UnitTesting
open SFS.Core.ConnectionHandler
open SFS.Core.Routing
open SFS.Core.ContentTypes
open SFS.Core.Logging

module ConnectionHandler =
    
    [<TestClass>]
    type TestClass () =

        let createRouteEmp =
            { paths = Seq.empty
              contentType = Html
              routeType = RouteType.Static
              content = None }
        
        let createErrorRoutes = {
            notFound = createRouteEmp
            internalError = createRouteEmp
            badRequest = createRouteEmp
            unauthorized = createRouteEmp
        }
        
        let createContext (routes:Map<string,Route>) errorRoutes =
            
            let logger = Logger()
            
            { logger = logger
              routes = routes
              errorRoutes = errorRoutes }
        
        [<TestMethod>]
        member this.TestMethodPassing () =
            
            let context = createContext Map.empty createErrorRoutes
            
            
            
            Assert.IsTrue(true);

