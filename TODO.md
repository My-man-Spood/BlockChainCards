1. Convert the App to a CLI
* [x] Integrate a CLI parser (e.g., System.CommandLine or CommandLineParser).
* [x] Define commands: create-user, mint-card, make-transaction, show-blockchain, etc.
* [x] Refactor Main to dispatch actions based on CLI input.
---
2. User Creation
* [x] Implement create-user command.
* [x] Generate a public/private key pair for the user (e.g., using ECDsa).
* [x] Save the private key securely (e.g., to a file, path provided by user).
* [x] Output or register the public key (e.g., save to a known directory or publish to a registry file).
---
3. Minting Cards
* [x] Implement mint-card command (can be restricted to issuer).
* [x] Require issuer’s private key path as an argument.
* [x] Load issuer’s private key, sign the mint transaction.
* [x] Add the mint transaction to the blockchain.
* [ ] Optionally, consider a separate CLI app for issuer actions if you want stricter separation.
---
4. User Transactions (Atomic Swaps)
* [ ] Implement make-transaction command.
* [ ] Allow specifying both parties, cards to exchange, and private key paths.
* [ ] Support a two-step process:
	* Step 1: Initiator creates and signs the transaction, outputs a pending transaction file.
    * Step 2: Counterparty loads the pending transaction, reviews, signs, and submits it.
* [ ] Ensure both signatures are present before adding to the blockchain.
---
5. Blockchain Storage
* [x] Implement blockchain persistence to a flat file (e.g., JSON or binary).
* [ ] On startup, load the blockchain from file; on changes, save it.
* [ ] Add a show-blockchain command to display the chain or its summary.
---
6. Issuer Authority Validation
* [ ] Store the issuer’s public key in a well-known location (e.g., a config file or hardcoded).
* [ ] When minting, verify that the transaction is signed by the issuer’s private key and matches the known public key.
* [ ] Reject mint transactions not signed by the issuer.
---
Optional Enhancements
* [ ] Add card definition registry (mapping card IDs to metadata).
* [ ] Add user-friendly error messages and help output for CLI.
* [ ] Implement transaction and block validation logic.
* [ ] Add logging for actions and errors.
---
Tip:
Start with the CLI skeleton and user creation, then add minting, then transactions, then persistence and validation. Test each step interactively as you go.