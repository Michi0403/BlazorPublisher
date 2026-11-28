# PublisherStudio 4.8.5

- Retry Apple's intermittent `notarytool history` HTTP 403 only when it explicitly reports that a required agreement is missing or expired; unrelated HTTP 403 responses remain fatal.
- Reuse the established macOS release-trust preflight retry policy (up to five attempts with 10-second delays); no notarization, signing or credential checks are skipped.
- Preserve the diagnostic output from every unsuccessful attempt in the terminal failure instead of reporting only the final attempt.
- Update application/installer version and documentation metadata consistently. No .NET compilation, signing or notarization was performed in this source-only delivery.
