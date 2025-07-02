# Clone metadata only (no files)
git clone --filter=blob:none --sparse https://github.com/terence-h/freshbasket-backend.git
cd freshbasket-backend

# Enable sparse-checkout and pull only 'User.Service'
git sparse-checkout init --cone
git sparse-checkout set User.Service

# Optionally pull the latest (required to get files)
git fetch origin
git switch <branch> # e.g., master