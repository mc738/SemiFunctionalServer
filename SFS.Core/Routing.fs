namespace SFS.Core

open System.Net.Mime
open SFS.Core.Utilities


module Routing =

    open System.IO
    open SFS.Core
    open ContentTypes
    open Http

    type Route =
        { paths: string seq
          contentType: ContentType
          routeType: RouteType
          content: byte array option }

    and RouteType = | Static
    //        | Dynamic
//
//    and DynamicHandler =
//        { name: string }
//        member this.Handler(request: Request) = [|0uy|]

    and RouteSetting =
        { contentType: ContentType
          routePaths: string seq
          contentPath: string
          routeType: RouteType }

    and HandlerType = | Static

    /// Create a static route.
    /// Because the content is static it is
    /// loaded into memory when the route is created.
    let createStaticRoute settings =

        match File.Exists(settings.contentPath) with
        | true ->
            let data = File.ReadAllBytes settings.contentPath
            Ok
                { paths = settings.routePaths
                  contentType = settings.contentType
                  routeType = settings.routeType
                  content = Some data }
        | false ->
            let message =
                sprintf "Could not load static content: '%s'." settings.contentPath

            Error message

    let createRoutes<'a, 'b> results =
        results
        |> Seq.map createStaticRoute
        |> Results.splitResults

    let createRouteMap (state: Map<string, Route>) (route: Route) =
        let newRoutes =
            route.paths
            |> Seq.map (fun r -> (r, route))
            |> Map.ofSeq

        Maps.join state newRoutes

    let createRoutesMap (routes: RouteSetting seq) =
        let (successful, errors) = createRoutes routes

        // TODO log any errors.
        successful |> Seq.fold createRouteMap Map.empty

    let matchRoute (routes: Map<string, Route>) (notFound: Route) (route: string) =
        match routes.TryFind route with
        | Some route -> route
        | None -> notFound