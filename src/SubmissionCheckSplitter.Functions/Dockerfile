# Use the SDK image to build your application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS installer-env
# FROM defradigital/dotnetcore-development:dotnet8.0 AS installer-env

# Copy everything
COPY SubmissionCheckSplitter.Functions/. ./SubmissionCheckSplitter.Functions/.
COPY SubmissionCheckSplitter.Application/. ./SubmissionCheckSplitter.Application/.
COPY SubmissionCheckSplitter.Data/. ./SubmissionCheckSplitter.Data/.
COPY stylecop.ruleset ./
COPY Directory.Build.props ./

RUN dotnet publish SubmissionCheckSplitter.Functions/*.csproj --output /home/site/wwwroot

FROM mcr.microsoft.com/azure-functions/dotnet:4-dotnet8
# FROM defradigital/dotnetcore:dotnet8.0

# Create a non-root user and set permissions
RUN groupadd -r dotnet && \
    useradd -r -g dotnet dotnet && \
    chown -R dotnet /home

# Switch to the non-root user for the final image
USER dotnet

# Set environment variables
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

# Copy the published application from the installer-env image
COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]