# Common ARM Templates

This folder contains generic ARM templates for common resources, etc.

The template multiplexer can be told to generate parameterized versions of any
of these templates by simply creating an empty file in a component's templates
folder with a name like
`vscs-componentname-{env}-{plane}-{instance}-{region}.@aks.template.json` .

The multiplexer will search _this_ templates folder for a common template named
`aks.template.json`, and then generate a parameterized copy of of the template
for each env/plane/instance/region of that component, for example:
`vscs-componentname-dev-ctrl-ci-as.aks.template.json`.

This allows editing commmon template pieces in one place.

> **NOTE**: the `@` is what tells the multiplexer to generate the template from a
common template. The env/plane/etc parameters in the file name are not required.
If they are missing, then only one template will be generated for that
component, for example:
`vscs-componentname.aks.template.json`.