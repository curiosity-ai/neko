1. **Add the `--multi-repo` flag to `Neko CLI`**:
   - In `Neko/Program.cs`, add a new `Option<bool>` for `--multi-repo`.
   - Update `watchCommand` to include this new option.
   - When `--multi-repo` is enabled:
     - Search the immediate subdirectories of the `--input` directory.
     - Look for subdirectories containing a `neko.yml` file.
     - Maintain a collection of `SiteBuilder` and `FileSystemWatcher` instances for each detected subdirectory.
     - Route requests using MapGet/UseFileServer configured appropriately for multiple folders if they are detected.

2. **Update `DevServer` to support multiple repos**:
   - We need to modify `DevServer` to map URLs based on the folder name.
   - Specifically, if `app/neko.yml` exists, the URL should be `localhost:port/app`.
   - Modify `DevServer` constructor or initialize method to accept a list of `(string RoutePrefix, string OutputDir, string InputDir)` configurations.
   - Use `app.MapWhen` or `UsePathBase` or mapping routes like `/app/neko-live` and mapping `app.UseFileServer` with `RequestPath = "/app"` for each detected repo.
   - Need to adjust the API endpoints (`/api/neko/content` and `/api/neko/upload-image`) to handle `RequestPath` logic or keep them global with paths reflecting the repo (e.g. mapping `path` to the correct `InputDir`).
   - Need to modify the rewrite rule in `DevServer` to handle prefix routes (so `/app/somefile` rewrites to `/app/somefile.html`).

3. **Update `watchCommand.SetHandler` logic**:
   - Handle `--multi-repo` flag logic.
   - Scan `input` directory for subdirectories containing `neko.yml`.
   - Build each one using `SiteBuilder`.
   - Keep a list of paths.
   - Start `DevServer` supporting all paths.
   - Watch files in each subdirectory and trigger rebuild and websocket reload for the affected directory or all directories.

4. **Verify changes**:
   - Test by running `dotnet run --project Neko watch --multi-repo` on a directory with multiple subfolders (each having a `neko.yml` and `index.md`).
   - Validate routing (e.g., `localhost:5000/app` correctly routes to `app`'s `index.html`).
   - Validate live reload works in multi-repo mode.
