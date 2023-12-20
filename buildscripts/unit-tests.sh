#!/usr/bin/env bash

dotnet test src/SubmissionCheckSplitter.UnitTests/SubmissionCheckSplitter.UnitTests.csproj --logger "trx;logfilename=testResults.trx"