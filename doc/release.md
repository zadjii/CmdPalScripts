# Release process

Thanks to the amazing work of
[@michaeljolley](https://github.com/michaeljolley), the release process is super
easy. Just create a new tag in the format `release/vX.X.X` and push it to the
repository. The workflow will automatically handle the rest, including signing
the packages and creating a release on GitHub.

```pwsh
# Create a new tag for the release
# This will prompt for the release notes. Have those ready.
git tag -a release/v0.0.2

# Push the tag to the repository
git push origin tag release/v0.0.2
```

The release workflow will: 
* Figure out the version from the tag, and replace the version in the package manifest
* Build the project
* Sign the MSIX packages using Azure Trusted Signing
* Create a GitHub release with the signed packages attached


## Notes on code signing

Just links, not a tutorial:

* https://github.com/Azure/trusted-signing-action
* https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure?tabs=azure-portal%2Cwindows#add-federated-credentials
  * DON'T DO THE MANAGED IDENTITY PART. 
  * DO THE [Register a Microsoft Entra app and create a service principal](https://learn.microsoft.com/en-us/entra/identity-platform/howto-create-service-principal-portal) one
* https://learn.microsoft.com/en-us/entra/identity-platform/howto-create-service-principal-portal#option-3-create-a-new-client-secret

[^1]: refer to https://github.com/michaeljolley/CmdPalExtensions/blob/main/.github/workflows/release-dadjokes.yml