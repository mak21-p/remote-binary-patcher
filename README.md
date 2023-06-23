# remote-binary-patcher

A remote binary patcher project built in C#'s ASP.NET. The purpose of this project was to make delta files on the fly that could be used to patch binary files to a newer version without having to redownload and overwrite the entire file.

## Technologies used

- Sqlite (database)
- ASP.NET (backend)
- Caddy (reverse proxy)
- Fastrsyncnet (delta file generation)
