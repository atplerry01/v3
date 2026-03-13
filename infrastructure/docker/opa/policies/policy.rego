package whyce.policy

default allow = false

allow {
    input.role == "operator"
}

allow {
    input.role == "admin"
}

allow {
    input.role == "system"
}
