# BlockChainCards TODO

## ✅ Completed
- [x] Wallet creation, loading, and saving
- [x] CLI parser and command structure
- [x] Mint transaction signing and authority validation
- [x] Trade (atomic swap) command and two-step signature flow
- [x] Blockchain persistence to JSON file
- [x] Show-blockchain CLI command
- [x] Fast SQLite-backed card ownership store

---

## 🟡 Card Ownership Store Integration (In Progress)
- [ ] Route all ownership checks and updates through `ICardOwnershipStore`
- [ ] Add error handling/logging for ingest failures
- [ ] Ensure checkpointing logic is robust (no missed/duplicate blocks)

---

## 🟠 Blockchain Storage Upgrade
- [ ] Migrate blockchain storage from JSON file to SQLite
    - [ ] Design schema for blocks and transactions
    - [ ] Implement efficient block/transaction insertion and queries
    - [ ] Update CLI and core logic to use new storage

---

## 🟢 Consensus, Mining, and Transaction Pool
- [ ] Add nonce and proof-of-work to blocks
    - [ ] Add nonce field to block structure
    - [ ] Implement mining logic (find valid nonce)
    - [ ] Add difficulty target and validation
- [ ] Implement transaction pool (mempool)
    - [ ] Maintain pool of pending transactions
    - [ ] Allow mining/confirmation from pool
    - [ ] Add CLI commands for submitting and viewing pending transactions
- [ ] Block confirmation and chain validation
    - [ ] Confirm blocks only after proof-of-work
    - [ ] Add full block and chain validation routines

---

## 🛠️ CLI, Usability, and Enhancements
- [ ] Add card definition registry (card IDs → metadata)
- [ ] Improve CLI error messages and help output
- [ ] Add logging for actions and errors
- [ ] (Optional) Separate CLI app for issuer actions

---

## 🧪 General & Integration
- [ ] Add more integration tests for all critical flows
- [ ] Review for any missing integration between new modules (ownership, storage, validation)
- [x] Store issuer’s public key in a well-known location
- [x] Reject mint transactions not signed by issuer

---

**Tip:**
Focus next on finishing card ownership store integration in `FileBlockChainReader`, especially validating during bulk ingest and robust error handling. Then, plan the SQLite blockchain storage migration and dive into mining, nonce, and transaction pool for a full blockchain experience!