# Code signing with SignPath

How LogExpert's binaries get an Authenticode signature through the SignPath Foundation
(free signing for open source), and what has to be set up on each side.

## How it works

```
build (Release) ──► upload bin/Release ──► SignPath signs our own exe/dlls ──► download
                                                                                  │
      signed installer ◄── SignPath signs setup.exe ◄── upload setup.exe ◄── Pack (no recompile)
```

- The signing key never touches GitHub. The workflow uploads the unsigned files as a GitHub
  workflow artifact, SignPath fetches it directly from GitHub (that is how it proves the files came
  from a real CI build of this repo), signs them and hands them back.
- The binaries are signed **before** packaging, so the zip, the Chocolatey package and the installer
  all contain signed files. The installer is then signed in a second request.
- Workflow: [.github/workflows/signpath_sign.yml](../../.github/workflows/signpath_sign.yml), started by
  hand (Actions → *Code Signing (SignPath)* → *Run workflow*).
- Artifact configurations (what to sign inside each upload) live only in the SignPath project.
  Their intended content is listed under [Step 1](#step-1-in-signpath-appsignpathio).

### What gets signed, and what doesn't

| Signed | Not signed | Why not |
|---|---|---|
| `LogExpert.exe`, `LogExpert.dll`, `LogExpert.*.dll`, `ColumnizerLib.dll` | Third-party DLLs (Newtonsoft.Json, NLog, DockPanelSuite, …) | SignPath Foundation only signs binaries built from our own source. |
| `LogExpert-Setup-<version>.exe` | Built-in plugins in `plugins/` and `pluginsx86/` | `PluginValidator` checks them against SHA256 hashes taken at compile time ([PluginHashGenerator.targets](../PluginRegistry/PluginHashGenerator.targets)). Signing changes the file bytes, so every built-in plugin would fail verification in Release. Fixing this needs the hashes to be computed *after* signing. That is a separate change. |
| | Inno Setup's uninstaller (`unins000.exe`) | Needs `SignTool` set up in the `.iss` script. Can be done later. |

## Phase 1: test certificate

### Step 1: In SignPath (app.signpath.io)

The menu names below may differ a little from the live UI.

1. **Organization ID**: a GUID, not the organization's slug. The UI hardly shows it. The easiest
   place to find it is the address bar: `app.signpath.io/Web/<organization-id>/...`.
2. **Trusted build system**: under *Trusted Build Systems*, add the predefined **GitHub.com** one,
   then link it to the LogExpert project.
3. **Project**: note the project's **slug**, the short identifier, not the display name.
4. **Signing policies**: check that a policy with the slug `test-signing` exists. The Foundation
   usually creates it, along with `release-signing`. If the slugs differ, change the `options:` list
   at the top of the workflow.
5. **Artifact configurations**: the project needs two.

   Slug `app-files`, for our own binaries. The root is `<zip-file>` because `actions/upload-artifact`
   zips `bin/Release`, and the paths are relative to that folder:

   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <artifact-configuration xmlns="http://signpath.io/artifact-configuration/v1">
     <zip-file>
       <pe-file-set>
         <include path="LogExpert.exe" />
         <include path="LogExpert.dll" />
         <include path="LogExpert.*.dll" max-matches="unbounded" />
         <include path="ColumnizerLib.dll" />
         <for-each>
           <authenticode-sign />
         </for-each>
       </pe-file-set>
     </zip-file>
   </artifact-configuration>
   ```

   Slug `initial` (display name "Initial Version"), for the installer. It is uploaded with
   `archive: false`, so the root is the exe itself:

   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <artifact-configuration xmlns="http://signpath.io/artifact-configuration/v1">
     <pe-file>
       <authenticode-sign />
     </pe-file>
   </artifact-configuration>
   ```

   Paste these by hand. Don't use a configuration that SignPath generates from an uploaded sample:
   it also lists XML files, and signing those (`<xml-file>`) is not part of the Foundation plan, so
   SignPath rejects it.
6. **API token**: create a CI user (or use your own user) with the **Submitter** role on the
   `test-signing` policy, and generate an API token for it. You need the token in the next step.
7. *(Optional)* Install the **SignPath GitHub App** on the repository. Not needed for a public repo,
   but needed later if you want pipeline policies such as "only sign builds from protected branches".

### Step 2: In GitHub (repo → Settings → Secrets and variables → Actions)

| Kind | Name | Value |
|---|---|---|
| Secret | `SIGNPATH_API_TOKEN` | the API token from step 1.6 |
| Variable | `SIGNPATH_ORGANIZATION_ID` | the organization ID from step 1.1 |
| Variable | `SIGNPATH_PROJECT_SLUG` | the project slug from step 1.3 |

The organization ID and slug are not secret, so they are stored as *variables* and stay visible in
logs.

### Step 3: Run and check

1. Merge the workflow to `Development`. `workflow_dispatch` workflows only show up in the Actions
   tab once they are on the default branch.
2. Actions → **Code Signing (SignPath)** → *Run workflow* → policy `test-signing`.
3. The run should:
   - print a table of signed binaries in *Check binary signatures*. With the test certificate the
     status is `UnknownError` (untrusted root), not `Valid`. That is expected.
   - finish with a `signed-packages-test-signing` artifact. The two unsigned artifacts are deleted
     at the end of a successful run, and kept when a step fails, so they can be inspected.
4. Download the artifact, right-click `LogExpert-Setup-*.exe` → *Properties* → *Digital Signatures*.
   A signature from the SignPath test certificate should be listed.
5. Install it and make sure LogExpert starts **and the built-in columnizers load**. This confirms
   that leaving the plugins unsigned kept the hash check working.

Each run also shows a link to its signing request in SignPath, under the signing step's outputs.

## Phase 2: release certificate

Once the test runs pass, the SignPath Foundation reviews the project before it allows
`release-signing`. Their conditions, per [signpath.org/terms](https://signpath.org/terms):

- **Code signing policy**: a section called "Code signing policy" on the project home page (the
  README) and on the release/download page. It must include:
  - the line *"Free code signing provided by [SignPath.io](https://about.signpath.io), certificate
    by [SignPath Foundation](https://signpath.org)"*
  - the team roles and who holds them: **Authors** (may change code without review),
    **Reviewers** (review outside contributions), **Approvers** (approve each signing request)
  - a privacy statement, for example: "This program will not transfer any information to other
    networked systems unless specifically requested by the user or the person installing or
    operating it."
- **MFA** turned on for every team member, on both GitHub and SignPath.
- **Manual approval**: every release-signing request waits for an Approver to click *Approve*
  in SignPath. The workflow waits up to 60 minutes (`wait-for-completion-timeout-in-seconds`).
- **Consistent metadata**: the product name must be the project name, and the product version must
  be the same on every signed file. `src/Directory.Build.props` sets `<Product>LogExpert</Product>`,
  and the Nuke `Compile` target stamps the GitVersion number onto every assembly, so this is
  already the case.

After that, either run the same workflow with `release-signing`, or move its steps into
[release.yml](../../.github/workflows/release.yml) between *Install InnoSetup* and
*Create GitHub Release*. Then every published release is signed automatically, after approval.
