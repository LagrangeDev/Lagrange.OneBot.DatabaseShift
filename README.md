# Lagrange.OneBot.DatabaseShift

```shell
Lagrange.OneBot.DatabaseShift --help
```

## Example

```shell
Lagrange.OneBot.DatabaseShift --in /path/to/lagrange-0.db
```

The output Realm database folder will be located at `/path/to/lagrange-0-db`.

## Other versions of Lagrange.OneBot

Checkout the branch of the submodule Lagrange.OneBot to the corresponding branch that uses LiteDB, modify RealmMessageRecord to ensure all desired fields are stored, then compile and run it.