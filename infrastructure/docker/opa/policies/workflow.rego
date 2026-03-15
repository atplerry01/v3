package whyce.workflow

default execute = false

execute {
    input.workflow == "vault.contribution"
}

execute {
    input.workflow == "ride.request"
}

execute {
    input.workflow == "property.listing"
}

execute {
    input.workflow == "economic.lifecycle"
}
