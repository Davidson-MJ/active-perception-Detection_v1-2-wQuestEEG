figure(2); clf;
itrial=8;
 dims = {'X', 'Y', 'Z'};
 
for idim=1:3
subplot(3,1,idim)
%plotY
plot(EyeDir(itrial).(dims{idim}) + HeadPos(itrial).(dims{idim}));
title(['EyeDir + headpos (' (dims{idim}) ')'])
hold on; pt=plot(TargPos(itrial).(dims{idim}), 'b', 'linew', 2);
legend(pt, 'Targetpos', 'autoupdate', 'off');
%
targshown = find(TargState(itrial).state);
tpos= TargPos(itrial).(dims{idim});

for ip = 1:length(targshown)
    plot(targshown(ip), tpos(targshown(ip)), 'o-r', 'linew', 4); hold on
end
end

%% try to plot screen view
numberOfSteps = length(trialTargPos.X);

ntargs = length(find(diff(targshown)>1)) +1;
targonsetst = [1,diff(targshown)'];
targonsetst = find(targonsetst>1);
targonsets = [1, targonsetst];
%%
clf
pEHy = HeadPos(itrial).Y + EyeDir(itrial).Y;
pEHx = HeadPos(itrial).Z + EyeDir(itrial).Z;

pTargy=TargPos(itrial).Y;
pTargx=TargPos(itrial).Z ;

 linespecs = [useCols{postoPlot} 'o-'];
        LineWidth = 3;
for itarg = 3%:length(targonsets)-1
    
    %plot trialtarget position.
    timevec= targshown(targonsets(itarg):targonsets(itarg+1)-1);
   timevecT = timevec(1)-3: timevec(2)+3;
%    
    
   
    xCoords = pEHx(timevecT);
    yCoords= pEHy(timevecT); hold on;         
	plot(xCoords, yCoords, 'LineWidth', 1, 'color', 'k');
    %start pos:
    plot(xCoords(1), yCoords(1), 'xr');
      xCoords = pEHx(timevec);
      yCoords= pEHy(timevec); hold on;         
	plot(xCoords, yCoords,  linespecs);    
	hold on;
%     xlim([1.88 1.9]); ylim([1.85 1.86]);
    
    %overlay target pos.
    axis tight
    xCoords = pTargx(timevecT);
    yCoords= pTargy(timevecT); hold on;
    plot(xCoords, yCoords, 'LineWidth', 1, 'color', 'b');
    
    %start pos:
    plot(xCoords(1), yCoords(1), 'xb');
    
    xCoords = pTargx(timevec);
    yCoords= pTargy(timevec); hold on;         
	plot(xCoords, yCoords,  linespecs);    
    
    xlim([-.1 .1])
    ylim([1.84 1.87])
end
%% ;
xy = zeros(numberOfSteps,2);
%
clf
plottimes =  [0,1,2,3,4,5,6,7,8];
texttimes = [2; dsearchn([TargState(itrial).times],  [1,2,3,4,5,6,7,8]')];

posData = {trialTargPos, trialEyePos, trialEyeDir};
useCols = {'b', 'k', 'm'};
for postoPlot=[1,3]
    subplot(3,1,postoPlot);
    pdata = posData{postoPlot};
ic=1;
for iframe = 2 : numberOfSteps
	% Walk in the x direction.
	
	% Now plot the walk so far.
	xCoords = pdata.Z(iframe-1:iframe);
	yCoords = pdata.Y(iframe-1:iframe);
    if TargState(itrial).state(iframe)==1
        
        linespecs = [useCols{postoPlot} 'o-'];
        LineWidth = 3;
        
    else
        linespecs = [useCols{postoPlot} '.'];        
        LineWidth = 1;
    end
        
	plot(xCoords, yCoords,  linespecs, 'LineWidth', LineWidth);
	hold on;
    
    if ismember(iframe, texttimes)
        text(xCoords,yCoords, [num2str(plottimes(ic))]);
    ic=ic+1;    
    end
end %each frame
axis tight
end %datatype