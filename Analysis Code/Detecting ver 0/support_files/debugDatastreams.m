%data debug check.
clf
for itrial = 3
frame_TC = squeeze(TargClickmatrix(:,itrial,:));
time_ax= avTime(itrial,:);

plot(time_ax, frame_TC(1,:), 'k'); hold on;
plot(time_ax, frame_TC(2,:), 'r'); hold on;

%collect summary info for comparison:
tos= trial_TargetSummary(itrial).targOnsets;
rts = trial_TargetSummary(itrial).clickOnsets;
fas = trial_TargetSummary(itrial).FalseAlarms;
title({['Summary: tons = ' num2str(tos')];[
   'RTs: ' num2str(rts')]} );

end

shg