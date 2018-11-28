### Git extension that opens pull requests associated with the current branch

This Git extension is developed as a .NET core CLI tool.

To install execute the folowing:
```
dotnet tool uninstall --global GitPullRequest
```

From a repository that has a pull request associated with the current branch:
```
git pr
```

The associated pull request will be opened in the default browser.
