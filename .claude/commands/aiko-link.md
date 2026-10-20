Link this project to another one in the Aiko registry, so work that belongs to the neighbour can be
filed there instead of here.

Ask the user which project and what it is for. The description is what a later agent reads to decide
when the neighbour is the right place for a piece of work, so it is written in the user's own words, as
a sentence about that project - not as a label like "other project".

Ask where the neighbour's reference lives as well, and when work does and does not belong there. Those
three answers are optional, but the reference is what an agent needs before it reaches for something the
neighbour owns - a library, a component, a pattern: without it the agent infers an API from whatever
package this project installed, which is not the same thing. A path or an address, in the user's words.

Call aiko_list_projects to find the project by its readable handle, then aiko_link_project with the
project whose registry is written, the project to link, that description and the three texts. Linking a
project to itself is refused, and so is linking one nobody has registered. A link can be removed with
aiko_unlink_project: that stops future routing and leaves the cards already filed there alone.

The links of the current project are listed by aiko_list_links, so an agent working here sees them
without asking - and can read them again without reading the whole project context.

<!-- Managed by Aiko -->
