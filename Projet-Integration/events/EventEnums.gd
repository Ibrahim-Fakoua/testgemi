extends Node

## Class that stores all enums linked to events
class_name EventEnums

## The different types of event a TypedEvent can be
const EventType := {
	HUNGER_TIMER = "HUNGER_TIMER",
	FATIGUE_TIMER = "FATIGUE_TIMER",
	STATE_EVENT = "STATE_EVENT",
	ACTION_EVENT = "ACTION_EVENT"
}

## Determines the order in which events get executed in the same timestamp
enum Priority {
	FIRST,
	NO_PRIORITY,
	LAST
}
