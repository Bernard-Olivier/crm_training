var Contact = window.Contact || {};
(function () {
  const statusCodes = {
    ACTIVE: "0",
    INACTIVE: "1",
  };

  const statusReason = {
    MATURED: "2",
  };

  const logicalNames = {
    STATUS: "statecode",
    STATUS_REASON: "statusreason",
    JOIN_DATE: "ss_joiningdate",
    MATURED_DATE: "ss_maturitydate",
  };

  this.setMatured = (formContext) => {
    console.log("test");
    // get values
    const statusCodeAttribute = formContext.getAttribute(logicalNames.STATUS);
    const statusCodeValue = statusCodeAttribute.getValue();
    const statusReasonAttribute = formContext.getAttribute(logicalNames.STATUS_REASON);
    const statusReasonValue = statusCodeAttribute.getValue();
    const joinDateAttribute = formContext.getAttribute(logicalNames.JOIN_DATE);
    const joinDateValue = statusCodeAttribute.getValue();
    const maturityDateAttribute = formContext.getAttribute(logicalNames.MATURED_DATE);
    const maturityDateValue = statusCodeAttribute.getValue();
    console.log("Join date: ", joinDateValue);
    // check if investment has matured and if status is active
    if (true) {
      // set status and status reason
      statusCodeAttribute.setValue(statusCodes.INACTIVE);
      statusReasonAttribute.setValue(statusReason.MATURED);
    }
    Xrm.Page.data.save();
    Xrm.Page.data.refresh();
  };
}).call(Contact);
