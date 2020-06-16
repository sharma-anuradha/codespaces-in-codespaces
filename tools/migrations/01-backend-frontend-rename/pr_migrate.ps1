#Requires -Version 7.0
param (
    [string]$sourceBranch,
    [bool]$branchFromRemote = $true,
    [string]$repoPath = ".",
    [string]$targetBranch = "origin/master"
)

$ErrorActionPreference = "Stop"

$scriptName = (Get-Item $PSCommandPath).Name
$scriptFolder = (Get-Item $PSCommandPath).DirectoryName
$modulesFolder = Resolve-Path (Join-Path $scriptFolder .. modules)

$branchSuffix = "-post-reorg"

$reorgPullRequestCommitMsg = "Merged PR 252943:"
$firstReorgCommitMsg = "Remove unused src/services/lib"
$lastReorgCommitMsg = "s/FrontEnd/Codespaces"

. (Join-Path $modulesFolder Git Git.ps1)

$migrateScriptName = "migrate.ps1"
$migrateScriptPath = Join-Path $scriptFolder $migrateScriptName
if (!(Test-Path $migrateScriptPath)) {
    Write-Error -Message "${migrateScriptName} is missing. It should be located adjacent to this script."
    return
}

$usage = "
Usage:
  ${scriptName} -sourceBranch {sourceBranch} -branchFromRemote {`$true|`$false} -repoPath {repoPath} -targetBranch {targetBranch}

sourceBranch
    The name of the branch that needs to be adapted to recent repo changes.
    After running this script, a new branch called {sourceBranch}${branchSuffix}
    will be created that has all changes. The original branch is not touched.

branchFromRemote
    Defaults to `$true. Indicates whether {sourceBranch}${branchSuffix}
    should branch from origin/{sourceBranch} or from the local {sourceBranch}.

repoPath
    Defaults to the current directory. Override if necessary to set the path to
    the git repo.

targetBranch
    Defaults to 'origin/master'. Represents the upstream branch where
    reorganization has already occurred. Should not generally be overridden.

This script effectively does the following:

1. Copy {sourceBranch} to a new branch called {sourceBranch}${branchSuffix}.
2. Merge the last commit before the reorganization occurred into
   {sourceBranch}${branchSuffix}.
3. Run the migrate.ps1 script to trigger the same reorganization in the new
   branch.
4. Reset the branch so that its parent commit is the reorganization commit.
   Now all the changes left are the changes from {sourceBranch}.
5. Squash all changes into a single commit. {sourceBranch}${branchSuffix}
   should now basically be {sourceBranch} rebased on top of the reorganization
   commit.

If any error occurs during merge (step 2), you should perform your own merge
(or rebase, if it's easier) before running this script and handle your own
conflicts. To do this, look in the git log for master and find the commit
with message starting with `"${reorgPullRequestCommitMsg}`". Merge or rebase
on the commit just before that, handle any conflicts, and rerun this script.

If other errors occur, please save your output and contact the author of this
script.

The original branch is not touched by this script. If you are satisfied with
the new branch, you can do the following to overwrite your original branch:

git checkout {sourceBranch}
git reset --hard {sourceBranch}${branchSuffix}
git push -f origin HEAD
"

if ($sourceBranch -eq "") {
    Write-Host $usage
    return
}

$repoPath = Resolve-Path $repoPath

$branchToBranch = $sourceBranch
if ($branchFromRemote) {
    $branchToBranch = "origin/${sourceBranch}"
}

try {
    Push-Location $repoPath

    _Git fetch origin

    # Fail early if source branch doesn't exist
    _Git checkout $branchToBranch
    $squashedAuthor = _Git --no-pager show -s --format='%an <%ae>' HEAD

    $reorgPullRequestCommit = _Git log --format="%H" --grep="${reorgPullRequestCommitMsg}" $targetBranch
    
    if($null -eq $reorgPullRequestCommit) {
        $lastReorgCommit = _Git log --format="%H" --grep="${lastReorgCommitMsg}" $targetBranch
        $firstReorgCommit = _Git log --format="%H" --grep="${firstReorgCommitMsg}" $targetBranch
    } else {
        # In the normal case, the PR commit does exist, and the individual
        # first/last commits have been squashed.
        $lastReorgCommit = $reorgPullRequestCommit
        $firstReorgCommit = $reorgPullRequestCommit
    }

    if ($null -eq $lastReorgCommit || $null -eq $firstReorgCommit) {
        Write-Error -Message "Reorg commit(s) not found in ${targetBranch} in ${repoPath}. Ensure {repoPath} and {targetBranch} are correct."
    }

    $updatedBranch = "${sourceBranch}${branchSuffix}"

    # If this is a rerun, trash the old version of the branch
    try {
        _Git branch -D $updatedBranch
    }
    catch {}

    _Git checkout -B $updatedBranch $branchToBranch

    $rebaseTarget = "${firstReorgCommit}^"
    # Use merge here instead of rebase because it's more reliable when devs
    # have been frequently merging master into their feature branch.
    _Git merge $rebaseTarget

    # Exclude merge commits from log for a better chance at a useful message
    # (in the single commit PR branch scenario).
    $squashedLog = _Git log --no-merges --format=%B "${rebaseTarget}.."
    $squashedLog = $squashedLog -join "`n"

    # Run the migration script
    pwsh.exe -File "${migrateScriptPath}" -repoPath $repoPath
    if ($? -eq $false) {
        Write-Error "There was an error running ${migrateScriptPath}. See output for details."
        return
    }

    # Finally, commit the squashed changes using original logs/author
    _Git reset --soft $lastReorgCommit
    # Except, undo deletion of migration scripts...
    _Git restore --staged --worktree tools/migrations
    _Git commit --author="${squashedAuthor}" -m "${squashedLog}"
}
finally {
    Pop-Location
}