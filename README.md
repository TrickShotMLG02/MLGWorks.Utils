# Git Submodule Guide

This guide explains how to **add**, **remove**, and **update** git submodules in your repository.

---

## Adding a Submodule

To add a submodule to your repo at a specific path:

1. Add the repo as submodule
```bash
git submodule add https://github.com/TrickShotMLG02/MLGWorks.Utils.git Assets/MLGWorks/MLGWorks.Utils
```

2. Commit the changes:

```bash
git commit -m "Remove submodule MLGWorks.Utils"
```

This will add the submodule and track it in your repository.

---

## Removing a Submodule

To remove a submodule:

1. Remove the submodule entry from `.gitmodules`:

```bash
git submodule deinit -f Assets/MLGWorks/MLGWorks.Utils
```

2. Remove the submodule directory from the working tree and the git index:

```bash
rm -rf Assets/MLGWorks/MLGWorks.Utils
git rm -f Assets/MLGWorks/MLGWorks.Utils
```

3. Commit the changes:

```bash
git commit -m "Remove submodule MLGWorks.Utils"
```

---

## Updating a Submodule

To update the submodule to the latest commit on its tracked branch:

```bash
git submodule update --remote Assets/MLGWorks/MLGWorks.Utils
```

Or, to update all submodules:

```bash
git submodule update --remote --merge
```

Then commit the updated submodule pointer:

```bash
git add Assets/MLGWorks/MLGWorks.Utils
git commit -m "Update submodule MLGWorks.Utils"
```

---

## Additional Commands

- Initialize submodules after cloning:

```bash
git submodule update --init --recursive
```

- Pull changes recursively for submodules:

```bash
git pull --recurse-submodules
```

---

That's it!  
Happy coding!
