### Git extension that opens pull requests associated with the current branch

This Git extension is developed as a .NET core CLI tool.

To install execute the folowing:
```
dotnet tool uninstall --global GitPullRequest
```

To browse or create a pull request:
```
git pr
```

If there is a pull request associated with the current branch is will be opened in the
default browser. If there is a remote branch but no pull request, the compare/create pull
request page will be opened.

### Other `git pr` options

There are other commands for listing pull requests or opening a specific pull request.:
```
# List local branches with an associated pull request
git pr --list

# Show pull request 7
git pr --list 7

# Browse pull request 7
git pr 7

# Browse pull request associated with current branch
git pr
```

### Feedback

If this command ever breaks or doesn't do what you expect, please open an issue or `@` me on
Twitter [https://twitter.com/jcansdale](https://twitter.com/jcansdale). I'll be keen fix or understand
what's going wrong.
