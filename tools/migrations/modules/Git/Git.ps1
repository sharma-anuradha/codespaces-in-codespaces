Function _Git
{
  & git @Args
  if ($LASTEXITCODE -ne 0) {
      Write-Error "'git $Args' exited $LASTEXITCODE"
  }
}