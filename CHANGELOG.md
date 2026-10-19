v1.2.0
- New/Renew/Reissue requests with a prior certificate are resolved from the SSL Store order expiry: reissued under the active order outside the renewal window (RenewalWindowDays, default 30), renewed inside it, ordered new once the order has expired. Supports shorter-lived (~200 day) certificates
- Removed the 'cannot renew an expired cert' failure; an expired order is now ordered as a new certificate
- Credentials and tokens (AuthToken, PartnerCode, Token, TokenCode, TokenID, passwords) are now masked in all Trace log output
- Enrollments now return a unique CA request ID ({order id}|{partner order id}, so repeated reissues of one order don't collide on the gateway's unique CARequestID index) and are reported as pending (awaiting validation) so Command keeps and polls them; GetSingleRecord returns the certificate once the order is issued and accepts order-serial request IDs
- SSL Store errors returned with HTTP 200 (for example the hourly API quota) are now reported as failures in sync, order status and GetSingleRecord instead of looking like a successful sync
- Prior certificate serial numbers now match with or without the leading 00 sign byte, so renewals of certificates whose serial starts with 0x80 or higher are found
- Added FlowLogger step diagrams to the Trace log for Enroll, Synchronize, Revoke, GetSingleRecord and Initialize
- Hardening: clearer errors when the prior certificate or its order cannot be found, SSL Store calls surface the original exception, per-step failure reporting in the flow log

v1.1.1
- SSL Store Api Changed Encoding Rules, needed to fix integration to match
  
v1.1.0
- Added new AutoWWW field for single domain SSL Store products

v1.0.4:
- Original Release Version
